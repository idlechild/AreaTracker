using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AreaTracker
{
   public class AreaColors
   {
      public readonly string Name;
      public readonly Color FillColor;
      public readonly Color TextBackgroundColor;

      public AreaColors(string name, Color fillColor, Color textBackgroundColor)
      {
         Name = name;
         FillColor = fillColor;
         TextBackgroundColor = textBackgroundColor;
      }
   }

   public partial class MainForm : Form
   {
      private readonly AreaColors DefaultAreaColors;
      private readonly int MapWidth;
      private readonly int MapHeight;
      private readonly int MapMargin;
      private readonly int BorderSize;

      private List<StateMap> m_AreaMaps;
      private List<StateMap> m_BossMaps;
      private List<AreaColors> m_AreaColors;
      private List<AreaColors> m_BossColors;
      private Font m_AreaFont;
      private Font m_BossFont;
      private Color m_BackgroundColor;
      private Color m_BorderColor;
      private Color m_SelfLinkColor;

      public MainForm(String searchPath)
      {
         SuspendLayout();
         MapWidth = -1;
         MapHeight = -1;
         MapMargin = -1;
         BorderSize = -1;
         InitializeMapData(searchPath, ref MapWidth, ref MapHeight, ref MapMargin, ref BorderSize);
         DefaultAreaColors = new AreaColors("", m_SelfLinkColor, Color.Transparent);
         InitializeRendering();
         InvalidateMaps(BitmapType.HitTest);
         ResumeLayout(false);
      }

      private void InitializeMapData(string searchPath, ref int mapWidth, ref int mapHeight, ref int mapMargin, ref int borderSize)
      {
         m_AreaMaps = new List<StateMap>();
         m_BossMaps = new List<StateMap>();
         m_AreaColors = new List<AreaColors>();
         m_BossColors = new List<AreaColors>();
         m_AreaFont = null;
         m_BossFont = null;
         m_BackgroundColor = Color.Transparent;
         m_BorderColor = Color.Transparent;
         m_SelfLinkColor = Color.Transparent;

         try
         {
            // Load the basic map configuration
            LoadMapConfiguration(searchPath + "\\All.conf", ref mapWidth, ref mapHeight, ref mapMargin, ref borderSize);

            // Load the map files
            string[] files = Directory.GetFiles(searchPath, "*.txt");
            foreach (string file in files)
            {
               try
               {
                  StateMap stateMap = new StateMap(file, MapWidth, MapHeight);
                  if ("Boss" == stateMap.Area)
                  {
                     m_BossMaps.Add(stateMap);
                  }
                  else
                  {
                     m_AreaMaps.Add(stateMap);
                  }
               }
               catch (MapException me)
               {
                  MessageBox.Show(me.Text, "Error",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
               }
            }

            // Build a list of missing data
            FindMissingData();
         }
         catch (MapException me)
         {
            MessageBox.Show(me.Text, "Error",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
         }
         catch (Exception e)
         {
            MessageBox.Show(e.Message, "Error",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
         }
      }

      private void LoadMapConfiguration(string filename, ref int mapWidth, ref int mapHeight, ref int mapMargin, ref int borderSize)
      {
         int commentIndex;
         int commaIndex;
         string keyword;
         string line;
         int lineCount = 0;
         StreamReader sr = new StreamReader(filename);

         lineCount++;
         line = sr.ReadLine();
         while (line != null)
         {
            string originalLine = line;
            commentIndex = line.IndexOf('#');
            if (commentIndex >= 0)
            {
               line = line.Remove(commentIndex);
            }

            line = line.Trim();
            commaIndex = line.IndexOf(',');
            if (commaIndex > 0)
            {
               keyword = line.Substring(0, commaIndex);
               line = line.Remove(0, commaIndex + 1);
            }
            else
            {
               keyword = line;
               line = "";
            }

            if (("Area" == keyword) ||
                ("Boss" == keyword))
            {
               commaIndex = line.IndexOf(',');
               if (commaIndex > 0)
               {
                  string name = line.Substring(0, commaIndex);
                  line = line.Remove(0, commaIndex + 1);
                  commaIndex = line.IndexOf(',');
                  if (commaIndex > 0)
                  {
                     try
                     {
                        uint fillColorValue = 0xFF000000 | UInt32.Parse(line.Substring(0, commaIndex), System.Globalization.NumberStyles.AllowHexSpecifier);
                        uint textBackroundColorValue = 0xFF000000 | UInt32.Parse(line.Substring(commaIndex + 1), System.Globalization.NumberStyles.AllowHexSpecifier);
                        AreaColors colors = new AreaColors(name, Color.FromArgb((int)fillColorValue), Color.FromArgb((int)textBackroundColorValue));
                        if ("Boss" == keyword)
                        {
                           m_BossColors.Add(colors);
                        }
                        else
                        {
                           m_AreaColors.Add(colors);
                        }
                     }
                     catch (Exception)
                     {
                        sr.Close();
                        throw new MapException(String.Format("Map Configuration: [Line {0}] Invalid colors ({1})", lineCount, line));
                     }
                  }
               }
               else
               {
                  sr.Close();
                  throw new MapException(String.Format("Map Configuration: [Line {0}] Area name not found", lineCount, originalLine));
               }
            }
            else if (("BackgroundColor" == keyword) ||
                     ("BorderColor" == keyword) ||
                     ("SelfLinkColor" == keyword))
            {
               try
               {
                  uint colorValue = 0xFF000000 | UInt32.Parse(line, System.Globalization.NumberStyles.AllowHexSpecifier);
                  if ("BackgroundColor" == keyword)
                  {
                     m_BackgroundColor = Color.FromArgb((int)colorValue);
                  }
                  else if ("BorderColor" == keyword)
                  {
                     m_BorderColor = Color.FromArgb((int)colorValue);
                  }
                  else
                  {
                     m_SelfLinkColor = Color.FromArgb((int)colorValue);
                  }
               }
               catch (Exception)
               {
                  sr.Close();
                  throw new MapException(String.Format("Map Configuration: [Line {0}] Invalid colors ({1})", lineCount, line));
               }
            }
            else if (("AreaFontSize" == keyword) ||
                     ("BossFontSize" == keyword) ||
                     ("MapWidth" == keyword) ||
                     ("MapHeight" == keyword) ||
                     ("MapMargin" == keyword) ||
                     ("BorderSize" == keyword))
            {
               try
               {
                  int value = int.Parse(line);
                  if ("AreaFontSize" == keyword)
                  {
                     m_AreaFont = new Font(FontFamily.GenericSansSerif, value, FontStyle.Bold);
                  }
                  else if ("BossFontSize" == keyword)
                  {
                     m_BossFont = new Font(FontFamily.GenericSansSerif, value, FontStyle.Bold);
                  }
                  else if ("MapWidth" == keyword)
                  {
                     mapWidth = value;
                  }
                  else if ("MapHeight" == keyword)
                  {
                     mapHeight = value;
                  }
                  else if ("MapMargin" == keyword)
                  {
                     mapMargin = value;
                  }
                  else
                  {
                     borderSize = value;
                  }
               }
               catch (Exception)
               {
                  sr.Close();
                  throw new MapException(String.Format("Map Configuration: [Line {0}] Invalid value ({1})", lineCount, line));
               }
            }
            else if (keyword.Length > 0)
            {
               sr.Close();
               throw new MapException(String.Format("Map Configuration: [Line {0}] Invalid keyword ({1})", lineCount, keyword));
            }

            lineCount++;
            line = sr.ReadLine();
         }

         sr.Close();
      }

      private void FindMissingData()
      {
         int index;
         string errorText = "";
         List<string> missingItems = new List<string>();

         for (int areaIndex = 0; areaIndex < m_AreaMaps.Count; ++areaIndex)
         {
            string areaName = m_AreaMaps[areaIndex].Area;
            for (index = 0; index <= m_AreaColors.Count; index++)
            {
               if (index == m_AreaColors.Count)
               {
                  missingItems.Add(areaName);
               }
               else if (areaName == m_AreaColors[index].Name)
               {
                  break;
               }
            }
         }

         if (missingItems.Count > 0)
         {
            errorText += String.Format("Missing Area Colors:\n{0}", missingItems[0]);
            for (index = 1; index < missingItems.Count; index++)
            {
               errorText += String.Format(", {0}", missingItems[index]);
            }
            errorText += "\n\n";
            missingItems.Clear();
         }

         for (int bossIndex = 0; bossIndex < m_BossMaps.Count; ++bossIndex)
         {
            string bossName = m_BossMaps[bossIndex].Name;
            for (index = 0; index <= m_BossColors.Count; index++)
            {
               if (index == m_BossColors.Count)
               {
                  missingItems.Add(bossName);
               }
               else if (bossName == m_BossColors[index].Name)
               {
                  break;
               }
            }
         }

         if (missingItems.Count > 0)
         {
            errorText += String.Format("Missing Boss Colors:\n{0}", missingItems[0]);
            for (index = 1; index < missingItems.Count; index++)
            {
               errorText += String.Format(", {0}", missingItems[index]);
            }
            errorText += "\n\n";
            missingItems.Clear();
         }

         if (null == m_AreaFont)
         {
            errorText += "Missing Area Font Size\n\n";
         }
         if (null == m_BossFont)
         {
            errorText += "Missing Boss Font Size\n\n";
         }

         if (Color.Transparent == m_BackgroundColor)
         {
            errorText += "Missing Background Color\n\n";
         }
         if (Color.Transparent == m_BorderColor)
         {
            errorText += "Missing Border Color\n\n";
         }
         if (Color.Transparent == m_SelfLinkColor)
         {
            errorText += "Missing Self Link Color\n\n";
         }

         if (MapWidth < 0)
         {
            errorText += "Missing Map Width\n\n";
         }
         if (MapHeight < 0)
         {
            errorText += "Missing Map Height\n\n";
         }
         if (MapMargin < 0)
         {
            errorText += "Missing Map Margin\n\n";
         }
         if (BorderSize < 0)
         {
            errorText += "Missing Border Size\n\n";
         }

         if (errorText.Length > 0)
         {
            MessageBox.Show(errorText, "Missing Data",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
         }
      }

      private AreaColors GetAreaColors(int areaIndex)
      {
         AreaColors areaColors = DefaultAreaColors;
         string areaName = m_AreaMaps[areaIndex - m_BossMaps.Count].Area;
         for (int index = 0; index < m_AreaColors.Count; index++)
         {
            if (areaName == m_AreaColors[index].Name)
            {
               areaColors = m_AreaColors[index];
               break;
            }
         }
         return areaColors;
      }

      private AreaColors GetBossColors(int bossIndex)
      {
         AreaColors bossColors = DefaultAreaColors;
         string bossName = m_BossMaps[bossIndex].Name;
         for (int index = 0; index < m_BossColors.Count; index++)
         {
            if (bossName == m_BossColors[index].Name)
            {
               bossColors = m_BossColors[index];
               break;
            }
         }
         return bossColors;
      }
   }
}
