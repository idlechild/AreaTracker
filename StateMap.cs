using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace AreaTracker
{
   public class MapException : Exception
   {
      public readonly string Text;

      public MapException(string text)
      {
         Text = text;
      }
   }

   public class StateMap
   {
      public readonly string Area;
      public readonly string Name;

      private readonly Point[] m_MapCoordinates;
      private readonly Point m_TopLeft;
      private readonly Point m_BottomRight;
      private readonly Size m_Size;

      private int m_LinkedAreaIndex;
      private string m_LinkedAreaName;
      private AreaColors m_LinkedAreaColors;

      public StateMap(string filename, int mapWidth, int mapHeight)
      {
         m_MapCoordinates = new Point[0];
         m_TopLeft = new Point(int.MaxValue, int.MaxValue);
         m_BottomRight = new Point(int.MinValue, int.MinValue);
         m_Size = new Size(0, 0);
         m_LinkedAreaIndex = -1;
         m_LinkedAreaName = "";
         m_LinkedAreaColors = null;

         // extract the label
         int len = filename.LastIndexOf('\\');
         if ((len < 0) || !filename.EndsWith(".txt"))
         {
            throw new MapException(String.Format("Invalid filename\n{0}", filename));
         }
         string tag = filename.Substring(len + 1);
         Name = tag.Remove(tag.Length - 4).Trim();
         string[] parts = Name.Split(' ');
         if (parts.Length > 1)
         {
            Area = parts[0];
            Name = Name.Substring(Area.Length).Trim().Replace(" ", Environment.NewLine);
         }
         else
         {
            Area = Name;
         }

         // load the state data
         try
         {
            m_MapCoordinates = LoadFile(filename, mapWidth, mapHeight, ref m_TopLeft, ref m_BottomRight).ToArray();
            m_Size.Width = m_BottomRight.X - m_TopLeft.X;
            m_Size.Height = m_BottomRight.Y - m_TopLeft.Y;
         }
         catch (System.IO.FileNotFoundException fnfe)
         {
            throw new MapException(fnfe.Message);
         }
         catch (System.IO.IOException ioe)
         {
            throw new MapException(ioe.Message);
         }
         catch (System.FormatException fe)
         {
            throw new MapException(fe.Message);
         }
         catch (MapException me)
         {
            throw new MapException(me.Text);
         }
      }

      private List<Point> LoadFile(string filename, int mapWidth, int mapHeight, ref Point topLeft, ref Point bottomRight)
      {
         List<Point> mapCoordinates = new List<Point>();
         int commentIndex;
         string line;
         int lineCount = 0;
         StreamReader sr = new StreamReader(filename);

         lineCount++;
         line = sr.ReadLine();
         while (line != null)
         {
            commentIndex = line.IndexOf('#');
            if (commentIndex >= 0)
            {
               line = line.Remove(commentIndex);
            }

            line = line.Trim();
            if (line.Length > 0)
            {
               try
               {
                  mapCoordinates.Add(ParseCoordinates(filename, lineCount, line, mapWidth, mapHeight, ref topLeft, ref bottomRight));
               }
               catch (MapException me)
               {
                  sr.Close();
                  throw new MapException(me.Text);
               }
            }

            // move on to the next line
            lineCount++;
            line = sr.ReadLine();
         }

         // close the file
         sr.Close();

         if (mapCoordinates.Count < 3)
         {
            throw new MapException(String.Format("{0}: There must be at least three coordinates in each polygon", filename));
         }
         return mapCoordinates;
      }

      private Point ParseCoordinates(string filename, int lineCount, string line, int mapWidth, int mapHeight, ref Point topLeft, ref Point bottomRight)
      {
         int x = -1;
         int y = -1;

         // Find our x and y coordinate strings
         int commaIndex = line.IndexOf(',');
         if (commaIndex < 0)
         {
            throw new MapException(String.Format("{0}: [Line {1}] Invalid data ({2})", filename, lineCount, line));
         }
         else
         {
            string xCoordinateString = line.Substring(0, commaIndex);
            string yCoordinateString = line.Substring(commaIndex + 1);

            // get the X coordinate
            try
            {
               x = int.Parse(xCoordinateString);
            }
            catch
            {
               throw new MapException(String.Format("{0}: [Line {1}] Invalid X coordinate ({2})", filename, lineCount, xCoordinateString));
            }

            if (x < 0)
            {
               throw new MapException(String.Format("{0}: [Line {1}] X coordinate ({2}) cannot be negative)", filename, lineCount, x));
            }
            else if (x >= mapWidth)
            {
               throw new MapException(String.Format("{0}: [Line {1}] X coordinate ({2}) must be less than the map width ({3})", filename, lineCount, x, mapWidth));
            }

            // get the Y coordinate
            try
            {
               y = int.Parse(yCoordinateString);
            }
            catch
            {
               throw new MapException(String.Format("{0}: [Line {1}] Invalid Y coordinate ({2})", filename, lineCount, yCoordinateString));
            }

            if (y < 0)
            {
               throw new MapException(String.Format("{0}: [Line {1}] Y coordinate ({2}) cannot be negative)", filename, lineCount, y));
            }
            else if (y >= mapHeight)
            {
               throw new MapException(String.Format("{0}: [Line {1}] Y coordinate ({2}) must be less than the map height ({3})", filename, lineCount, y, mapHeight));
            }
         }

         // if we got this far, take this coordinate
         if (topLeft.X > x)
         {
            topLeft.X = x + 1;
         }
         if (bottomRight.X < x)
         {
            bottomRight.X = x - 1;
         }
         if (topLeft.Y > y)
         {
            topLeft.Y = y + 1;
         }
         if (bottomRight.Y < y)
         {
            bottomRight.Y = y - 1;
         }
         return new Point(x, y);
      }

      public int SetLink(int linkedAreaIndex, string linkedAreaName, AreaColors linkedAreaColors)
      {
         int previousLinkedAreaIndex = m_LinkedAreaIndex;
         m_LinkedAreaIndex = linkedAreaIndex;
         m_LinkedAreaName = linkedAreaName;
         m_LinkedAreaColors = linkedAreaColors;
         return previousLinkedAreaIndex;
      }

      public void Render(Graphics graphics, Pen pen, SolidBrush brush, Font font)
      {
         graphics.FillPolygon(brush, m_MapCoordinates);

         for (int index = 1; index < m_MapCoordinates.Length; index++)
         {
            graphics.DrawLine(pen,
               m_MapCoordinates[index - 1].X, m_MapCoordinates[index - 1].Y,
               m_MapCoordinates[index].X, m_MapCoordinates[index].Y);
         }

         graphics.DrawLine(pen,
            m_MapCoordinates[m_MapCoordinates.Length - 1].X, m_MapCoordinates[m_MapCoordinates.Length - 1].Y,
            m_MapCoordinates[0].X, m_MapCoordinates[0].Y);

         // Draw the abbreviation if we have one
         if ((m_LinkedAreaName.Length > 0) && (null != font))
         {
            Size sizeOfText = System.Windows.Forms.TextRenderer.MeasureText(m_LinkedAreaName, font);
            Point textLocation = m_TopLeft;
            if (sizeOfText.Width > m_Size.Width)
            {
               sizeOfText.Width = m_Size.Width;
            }
            else
            {
               textLocation.X += (m_Size.Width - sizeOfText.Width) / 2;
            }
            if (sizeOfText.Height > m_Size.Height)
            {
               sizeOfText.Height = m_Size.Height;
            }
            else
            {
               textLocation.Y += (m_Size.Height - sizeOfText.Height) / 2;
            }
            Rectangle rect = new Rectangle(textLocation, sizeOfText);
            graphics.FillRectangle(new SolidBrush(m_LinkedAreaColors.TextBackgroundColor), rect);
            graphics.DrawString(m_LinkedAreaName, font, new SolidBrush(m_LinkedAreaColors.FillColor), textLocation);
         }
      }
      
      public void AddEdges(List<StateEdge> edges)
      {
         for (int index = 1; index < m_MapCoordinates.Length; index++)
         {
            AddEdge(edges, new StateEdge(
               m_MapCoordinates[index - 1].X, m_MapCoordinates[index - 1].Y,
               m_MapCoordinates[index].X, m_MapCoordinates[index].Y));
         }

         AddEdge(edges, new StateEdge(
            m_MapCoordinates[m_MapCoordinates.Length - 1].X, m_MapCoordinates[m_MapCoordinates.Length - 1].Y,
            m_MapCoordinates[0].X, m_MapCoordinates[0].Y));
      }

      private void AddEdge(List<StateEdge> edges, StateEdge edgeToAdd)
      {
         bool isDuplicate = false;

         for (int index = 0; !isDuplicate && (index < edges.Count); index++)
         {
            isDuplicate = edges[index].IsDuplicate(edgeToAdd);
         }

         if (!isDuplicate)
         {
            edges.Add(edgeToAdd);
         }
      }
   }
}
