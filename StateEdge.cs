using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace AreaTracker
{
   public class StateEdge
   {
      private int m_X1;
      private int m_Y1;
      private int m_X2;
      private int m_Y2;
      private bool m_Duplicate;

      public StateEdge(int x1, int y1, int x2, int y2)
      {
         m_X1 = x1;
         m_Y1 = y1;
         m_X2 = x2;
         m_Y2 = y2;
         m_Duplicate = false;
      }

      public bool Duplicate
      {
         get
         {
            return m_Duplicate;
         }
      }

      public bool IsDuplicate(StateEdge stateEdge)
      {
         bool retVal = false;

         if (((m_X1 == stateEdge.m_X1) &&
              (m_Y1 == stateEdge.m_Y1) &&
              (m_X2 == stateEdge.m_X2) &&
              (m_Y2 == stateEdge.m_Y2)) ||
             ((m_X1 == stateEdge.m_X2) &&
              (m_Y1 == stateEdge.m_Y2) &&
              (m_X2 == stateEdge.m_X1) &&
              (m_Y2 == stateEdge.m_Y1)))
         {
            retVal = true;

            // We are now a duplicate as well
            m_Duplicate = true;
         }

         return retVal;
      }

      public void Render(Graphics graphics, Pen pen)
      {
         graphics.DrawLine(pen, m_X1, m_Y1, m_X2, m_Y2);
      }
   }
}
