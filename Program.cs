using System;
using System.IO;
using System.Windows.Forms;

namespace AreaTracker
{
   static class Program
   {
      [STAThread]
      static void Main(string[] args)
      {
         String searchPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
         int index = searchPath.IndexOf("\\bin\\");
         if (index > 0)
         {
            searchPath = searchPath.Substring(0, index);
         }
         else
         {
            index = searchPath.IndexOf(".exe");
            if (index > 0)
            {
               index = searchPath.LastIndexOf("\\");
               if (index > 0)
               {
                  searchPath = searchPath.Substring(0, index);
               }
            }
         }
         if (Directory.Exists(searchPath + "\\MapData"))
         {
            searchPath += "\\MapData";
         }

         if (6 == args.Length)
         {
            UpdateMapData(searchPath, args);
         }
         else
         {
            if (1 == args.Length)
            {
               searchPath = args[0];
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(searchPath));
         }
      }

      static void UpdateMapData(string searchPath, string[] args)
      {
         try
         {
            int originalMargin = int.Parse(args[0]);
            int originalHalfSize = int.Parse(args[1]);
            int xOffset = int.Parse(args[2]);
            int yOffset = int.Parse(args[3]);
            int newHalfSize = int.Parse(args[4]);
            int newMargin = int.Parse(args[5]);
            Directory.CreateDirectory(searchPath + "\\new");
            foreach (var filename in Directory.EnumerateFiles(searchPath, "*.txt"))
            {
               int pathIndex = filename.LastIndexOf('\\');
               bool bossArea = filename.Substring(pathIndex + 1).StartsWith("Boss ");
               StreamReader sr = new StreamReader(filename);
               StreamWriter sw = new StreamWriter(filename.Insert(pathIndex, "\\new"), false);
               int lineCount = 0;
               while (!sr.EndOfStream)
               {
                  string line = sr.ReadLine();
                  int commaIndex = line.IndexOf(',');
                  if (commaIndex > 0)
                  {
                     int x = int.Parse(line.Substring(0, commaIndex));
                     int y = int.Parse(line.Substring(commaIndex + 1));
                     switch (lineCount)
                     {
                        case 0:
                           if (bossArea)
                           {
                              y += originalMargin;
                           }
                           else
                           {
                              x -= originalMargin;
                              y -= originalMargin;
                           }
                           break;
                        case 1:
                           if (bossArea)
                           {
                              y += originalMargin;
                           }
                           else
                           {
                              x += originalMargin;
                              y -= originalMargin;
                           }
                           break;
                        case 2:
                           if (!bossArea)
                           {
                              x += originalMargin;
                           }
                           y += originalMargin;
                           break;
                        case 3:
                           if (!bossArea)
                           {
                              x -= originalMargin;
                           }
                           y += originalMargin;
                           break;
                        default:
                           break;
                     }
                     x /= originalHalfSize;
                     y /= originalHalfSize;
                     x += xOffset;
                     y += yOffset;
                     x *= newHalfSize;
                     y *= newHalfSize;
                     switch (lineCount)
                     {
                        case 0:
                           if (bossArea)
                           {
                              y -= newMargin;
                           }
                           else
                           {
                              x += newMargin;
                              y += newMargin;
                           }
                           break;
                        case 1:
                           if (bossArea)
                           {
                              y -= newMargin;
                           }
                           else
                           {
                              x -= newMargin;
                              y += newMargin;
                           }
                           break;
                        case 2:
                           if (!bossArea)
                           {
                              x -= newMargin;
                           }
                           y -= newMargin;
                           break;
                        case 3:
                           if (!bossArea)
                           {
                              x += newMargin;
                           }
                           y -= newMargin;
                           break;
                        default:
                           break;
                     }
                     sw.WriteLine("{0},{1}", x, y);
                     lineCount++;
                  }
               }
               sw.Close();
               sr.Close();
            }
         }
         catch (Exception e)
         {
            Console.Write("Error: {0}", e.ToString());
         }
      }
   }
}
