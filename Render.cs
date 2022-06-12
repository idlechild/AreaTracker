using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace AreaTracker
{
   public partial class MainForm : Form
   {
      private enum BitmapStatus
      {
         Valid,
         Invalid,
         Redraw
      }

      private enum BitmapType
      {
         HitTest,
         Map,
         Highlight
      }

      private Object m_BitmapLock;

      private Bitmap m_HitTestBitmap;
      private BitmapStatus m_HitTestStatus;

      private Bitmap m_MapBitmap;
      private BitmapStatus m_MapStatus;

      private int m_ClickedState;
      private int m_SelectedState;
      private int m_HighlightedState;
      private BitmapStatus m_HighlightStatus;

      private void InitializeRendering()
      {
         m_BitmapLock = new Object();

         m_HitTestBitmap = new Bitmap(MapWidth, MapHeight);
         m_HitTestStatus = BitmapStatus.Redraw;

         m_MapBitmap = new Bitmap(MapWidth, MapHeight);
         m_MapStatus = BitmapStatus.Redraw;

         m_ClickedState = int.MaxValue;
         m_SelectedState = int.MaxValue;
         m_HighlightedState = int.MaxValue;
         m_HighlightStatus = BitmapStatus.Redraw;

         var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
         this.BackColor = m_BackgroundColor;
         this.ClientSize = new System.Drawing.Size(MapWidth + 2 * MapMargin, MapHeight + 2 * MapMargin);
         this.LostFocus += new EventHandler(this.OnLostFocus);
         this.MouseMove += new MouseEventHandler(this.OnMouseMove);
         this.MouseUp += new MouseEventHandler(this.OnMouseUp);
         this.Margin = new System.Windows.Forms.Padding(MapMargin, MapMargin, MapMargin, MapMargin);
         this.Name = "MainForm";
         this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                       ControlStyles.UserPaint |
                       ControlStyles.DoubleBuffer, true);
         this.Text = String.Format("Area Tracker v{0}.{1}", version.Major, version.Minor);
      }

      private void OnLostFocus(object sender, EventArgs e)
      {
         HandleMouseMovementOnMap(-1, -1, false);
      }

      private void OnMouseMove(object sender, MouseEventArgs e)
      {
         HandleMouseMovementOnMap(e.X - MapMargin, e.Y - MapMargin, false);
      }

      private void OnMouseUp(object sender, MouseEventArgs e)
      {
         HandleMouseMovementOnMap(e.X - MapMargin, e.Y - MapMargin, true);
      }

      private void HandleMouseMovementOnMap(int x, int y, bool buttonPressed)
      {
         // See if the mouse was clicked on our map
         bool mouseOnMap = false;
         if ((x >= 0) && (x < MapWidth) && (y >= 0) && (y < MapHeight))
         {
            mouseOnMap = true;
         }

         // Get the selected index if the hit test bitmap is valid
         int selectedIndex = int.MaxValue;
         bool hitTestBitmapValid = false;
         lock (m_BitmapLock)
         {
            if (m_HitTestStatus == BitmapStatus.Valid)
            {
               hitTestBitmapValid = true;
               if (mouseOnMap)
               {
                  selectedIndex = m_HitTestBitmap.GetPixel(x, y).ToArgb() & 0x00FFFFFF;
                  if (selectedIndex >= (m_BossMaps.Count + m_AreaMaps.Count))
                  {
                     selectedIndex = int.MaxValue;
                  }
               }
            }
         }

         if (hitTestBitmapValid)
         {
            if (m_SelectedState != selectedIndex)
            {
               m_SelectedState = selectedIndex;
               InvalidateMaps(BitmapType.Highlight);
            }
            else if (buttonPressed)
            {
               if (int.MaxValue == m_ClickedState)
               {
                  if (m_SelectedState < m_BossMaps.Count)
                  {
                     CreateBossLink();
                  }
                  else
                  {
                     m_ClickedState = m_SelectedState;
                  }
               }
               else if ((int.MaxValue == m_SelectedState) ||
                        (m_SelectedState < m_BossMaps.Count) ||
                        (m_ClickedState < m_BossMaps.Count))
               {
                  m_HighlightedState = m_ClickedState;
                  m_ClickedState = int.MaxValue;
                  InvalidateMaps(BitmapType.Highlight);
               }
               else
               {
                  CreateAreaLink();
               }
            }
         }
      }

      private void CreateAreaLink()
      {
         bool invalidateMap = false;
         if (m_HighlightedState == m_ClickedState)
         {
            int previousLinkIndex = m_AreaMaps[m_ClickedState - m_BossMaps.Count].SetLink(-1, "", null);
            if (previousLinkIndex >= 0)
            {
               m_AreaMaps[previousLinkIndex - m_BossMaps.Count].SetLink(-1, "", null);
               invalidateMap = true;
            }
            m_HighlightedState = int.MaxValue;
         }
         else
         {
            AreaColors clickedColors = GetAreaColors(m_ClickedState);
            AreaColors highlightedColors = GetAreaColors(m_HighlightedState);
            if (clickedColors.FillColor == highlightedColors.FillColor)
            {
               clickedColors = DefaultAreaColors;
               highlightedColors = DefaultAreaColors;
            }
            int previousLinkIndex = m_AreaMaps[m_ClickedState - m_BossMaps.Count].SetLink(m_HighlightedState, m_AreaMaps[m_HighlightedState - m_BossMaps.Count].Name, highlightedColors);
            if ((previousLinkIndex >= 0) && (previousLinkIndex != m_HighlightedState))
            {
               m_AreaMaps[previousLinkIndex - m_BossMaps.Count].SetLink(-1, "", null);
               invalidateMap = true;
            }
            previousLinkIndex = m_AreaMaps[m_HighlightedState - m_BossMaps.Count].SetLink(m_ClickedState, m_AreaMaps[m_ClickedState - m_BossMaps.Count].Name, clickedColors);
            if ((previousLinkIndex >= 0) && (previousLinkIndex != m_ClickedState))
            {
               m_AreaMaps[previousLinkIndex - m_BossMaps.Count].SetLink(-1, "", null);
               invalidateMap = true;
            }
            m_HighlightedState = m_ClickedState;
         }
         m_ClickedState = int.MaxValue;
         InvalidateMaps(invalidateMap ? BitmapType.Map : BitmapType.Highlight);
      }

      private void CreateBossLink()
      {
         string bossName = "";
         int previousLinkIndex = m_BossMaps[m_HighlightedState].SetLink(-1, "", null);
         if ((previousLinkIndex >= 0) && (previousLinkIndex < m_BossMaps.Count))
         {
            bossName = m_BossMaps[previousLinkIndex].Name;
            for (int index = 0; index <= m_BossColors.Count; index++)
            {
               if (index == m_BossColors.Count)
               {
                  bossName = m_BossColors[0].Name;
               }
               else if (bossName == m_BossColors[index].Name)
               {
                  if ((index + 1) < m_BossColors.Count)
                  {
                     bossName = m_BossColors[index + 1].Name;
                  }
                  else
                  {
                     bossName = "";
                  }
                  break;
               }
            }
         }
         else if (m_BossColors.Count > 0)
         {
            bossName = m_BossColors[0].Name;
         }
         if (bossName.Length > 0)
         {
            for (int bossIndex = 0; bossIndex < m_BossMaps.Count; bossIndex++)
            {
               if (bossName == m_BossMaps[bossIndex].Name)
               {
                  m_BossMaps[m_HighlightedState].SetLink(bossIndex, bossName, GetBossColors(bossIndex));
               }
            }
         }
         m_HighlightedState = int.MaxValue;
         InvalidateMaps(BitmapType.Highlight);
      }

      private void InvalidateMaps(BitmapType type)
      {
         lock (m_BitmapLock)
         {
            m_HighlightStatus = BitmapStatus.Redraw;
            if ((type == BitmapType.Map) ||
                (type == BitmapType.HitTest))
            {
               m_MapStatus = BitmapStatus.Redraw;
               if (type == BitmapType.HitTest)
               {
                  m_HitTestStatus = BitmapStatus.Redraw;
               }
            }
         }
         Invalidate();
      }

      protected override void OnPaint(PaintEventArgs e) 
      {
         UpdateBitmaps();
         e.Graphics.DrawImageUnscaled(m_MapBitmap, MapMargin, MapMargin);
      }

      private void UpdateBitmaps()
      {
         BitmapStatus hitTestStatus;
         BitmapStatus mapStatus;
         BitmapStatus highlightStatus;

         // Check if we need to redraw a bitmap
         lock (m_BitmapLock)
         {
            // Clear the redraw flags now
            // That way we will know if the application sets it later
            if (m_HitTestStatus == BitmapStatus.Redraw)
            {
               m_HitTestStatus = BitmapStatus.Invalid;
            }
            if (m_MapStatus == BitmapStatus.Redraw)
            {
               m_MapStatus = BitmapStatus.Invalid;
            }
            if (m_HighlightStatus == BitmapStatus.Redraw)
            {
               m_HighlightStatus = BitmapStatus.Invalid;
            }

            hitTestStatus = m_HitTestStatus;
            mapStatus = m_MapStatus;
            highlightStatus = m_HighlightStatus;
         }

         // Keep going until we don't have to update either bitmap
         while ((hitTestStatus != BitmapStatus.Valid) ||
                (mapStatus != BitmapStatus.Valid) ||
                (highlightStatus != BitmapStatus.Valid))
         {
            if (hitTestStatus != BitmapStatus.Valid)
            {
               UpdateHitTestBitmap();
            }

            if (mapStatus != BitmapStatus.Valid)
            {
               UpdateMapBitmap();
            }

            if (highlightStatus != BitmapStatus.Valid)
            {
               UpdateHighlightBitmap();
            }

            lock (m_BitmapLock)
            {
               // If the redraw flag is set, then our bitmap is still invalid
               // Otherwise, we just redrew the bitmap so it is now valid
               if (m_HitTestStatus == BitmapStatus.Redraw)
               {
                  m_HitTestStatus = BitmapStatus.Invalid;
               }
               else
               {
                  m_HitTestStatus = BitmapStatus.Valid;
               }

               if (m_MapStatus == BitmapStatus.Redraw)
               {
                  m_MapStatus = BitmapStatus.Invalid;
               }
               else
               {
                  m_MapStatus = BitmapStatus.Valid;
               }

               if (m_HighlightStatus == BitmapStatus.Redraw)
               {
                  m_HighlightStatus = BitmapStatus.Invalid;
               }
               else
               {
                  m_HighlightStatus = BitmapStatus.Valid;
               }

               hitTestStatus = m_HitTestStatus;
               mapStatus = m_MapStatus;
               highlightStatus = m_HighlightStatus;
            }
         }
      }

      private void UpdateHitTestBitmap()
      {
         List<StateEdge> edges = new List<StateEdge>();
         Graphics graphics = Graphics.FromImage(m_HitTestBitmap);

         Color backgroundColor = Color.FromArgb(255, 255, 255);
         Pen backgroundPen = new Pen(backgroundColor, BorderSize);
         SolidBrush backgroundBrush = new SolidBrush(backgroundColor);
         Rectangle backgroundRect = new Rectangle(0, 0, MapWidth, MapHeight);
         graphics.FillRectangle(backgroundBrush, backgroundRect);

         // Draw each state
         Color color;
         Pen pen;
         SolidBrush brush;
         for (int index = 0; index < (m_BossMaps.Count + m_AreaMaps.Count); index++)
         {
            color = Color.FromArgb(index >> 16, index >> 8, index);
            pen = new Pen(color, BorderSize);
            brush = new SolidBrush(color);
            if (index < m_BossMaps.Count)
            {
               m_BossMaps[index].Render(graphics, pen, brush, m_BossFont);
               m_BossMaps[index].AddEdges(edges);
            }
            else
            {
               m_AreaMaps[index - m_BossMaps.Count].Render(graphics, pen, brush, m_AreaFont);
               m_AreaMaps[index - m_BossMaps.Count].AddEdges(edges);
            }
         }

         // Redraw duplicate edges
         for (int index = 0; index < edges.Count; index++)
         {
            if (edges[index].Duplicate)
            {
               edges[index].Render(graphics, backgroundPen);
            }
         }

         graphics.Dispose();

         // Nothing should be selected if the hit test map was just rebuilt
         m_ClickedState = int.MaxValue;
         m_SelectedState = int.MaxValue;
         m_HighlightedState = int.MaxValue;
      }

      private void UpdateMapBitmap()
      {
         Graphics graphics = Graphics.FromImage(m_MapBitmap);
         graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
         graphics.SmoothingMode = SmoothingMode.AntiAlias;

         SolidBrush backgroundBrush = new SolidBrush(m_BackgroundColor);
         Rectangle backgroundRect = new Rectangle(0, 0, MapWidth, MapHeight);
         graphics.FillRectangle(backgroundBrush, backgroundRect);
         Pen pen = new Pen(m_BorderColor, BorderSize);

         for (int index = 0; index < (m_BossMaps.Count + m_AreaMaps.Count); index++)
         {
            if (index < m_BossMaps.Count)
            {
               SolidBrush brush = new SolidBrush(GetBossColors(index).FillColor);
               m_BossMaps[index].Render(graphics, pen, brush, m_BossFont);
            }
            else
            {
               SolidBrush brush = new SolidBrush(GetAreaColors(index).FillColor);
               m_AreaMaps[index - m_BossMaps.Count].Render(graphics, pen, brush, m_AreaFont);
            }
         }

         graphics.Dispose();
      }

      private void UpdateHighlightBitmap()
      {
         if (m_HighlightedState != m_SelectedState)
         {
            Graphics graphics = Graphics.FromImage(m_MapBitmap);
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Pen pen = new Pen(m_BorderColor, BorderSize);

            // Redraw the previously highlighted state
            if (m_HighlightedState < (m_BossMaps.Count + m_AreaMaps.Count))
            {
               if (m_ClickedState == m_HighlightedState)
               {
                  if (m_HighlightedState < m_BossMaps.Count)
                  {
                     Color highlightedColor = Color.FromArgb(128, GetBossColors(m_HighlightedState).FillColor);
                     SolidBrush highlightedBrush = new SolidBrush(highlightedColor);
                     SolidBrush whiteBrush = new SolidBrush(Color.White);
                     m_BossMaps[m_HighlightedState].Render(graphics, pen, whiteBrush, m_BossFont);
                     m_BossMaps[m_HighlightedState].Render(graphics, pen, highlightedBrush, m_BossFont);
                  }
                  else
                  {
                     Color highlightedColor = Color.FromArgb(128, GetAreaColors(m_HighlightedState).FillColor);
                     SolidBrush highlightedBrush = new SolidBrush(highlightedColor);
                     SolidBrush whiteBrush = new SolidBrush(Color.White);
                     m_AreaMaps[m_HighlightedState - m_BossMaps.Count].Render(graphics, pen, whiteBrush, m_AreaFont);
                     m_AreaMaps[m_HighlightedState - m_BossMaps.Count].Render(graphics, pen, highlightedBrush, m_AreaFont);
                  }
               }
               else
               {
                  if (m_HighlightedState < m_BossMaps.Count)
                  {
                     SolidBrush brush = new SolidBrush(GetBossColors(m_HighlightedState).FillColor);
                     m_BossMaps[m_HighlightedState].Render(graphics, pen, brush, m_BossFont);
                  }
                  else
                  {
                     SolidBrush brush = new SolidBrush(GetAreaColors(m_HighlightedState).FillColor);
                     m_AreaMaps[m_HighlightedState - m_BossMaps.Count].Render(graphics, pen, brush, m_AreaFont);
                  }
               }
            }

            m_HighlightedState = m_SelectedState;

            // Redraw the state to be highlighted
            if (m_HighlightedState < (m_BossMaps.Count + m_AreaMaps.Count))
            {
               if (m_HighlightedState < m_BossMaps.Count)
               {
                  Color highlightedColor = Color.FromArgb(128, GetBossColors(m_HighlightedState).FillColor);
                  SolidBrush highlightedBrush = new SolidBrush(highlightedColor);
                  SolidBrush whiteBrush = new SolidBrush(Color.White);
                  m_BossMaps[m_HighlightedState].Render(graphics, pen, whiteBrush, m_BossFont);
                  m_BossMaps[m_HighlightedState].Render(graphics, pen, highlightedBrush, m_BossFont);
               }
               else
               {
                  Color highlightedColor = Color.FromArgb(128, GetAreaColors(m_HighlightedState).FillColor);
                  SolidBrush highlightedBrush = new SolidBrush(highlightedColor);
                  SolidBrush whiteBrush = new SolidBrush(Color.White);
                  m_AreaMaps[m_HighlightedState - m_BossMaps.Count].Render(graphics, pen, whiteBrush, m_AreaFont);
                  m_AreaMaps[m_HighlightedState - m_BossMaps.Count].Render(graphics, pen, highlightedBrush, m_AreaFont);
               }
            }

            graphics.Dispose();
         }
      }


  }
}
