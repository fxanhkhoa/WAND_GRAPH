using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAND_GRAPH
{
    public class Symbol
    {
        public struct axis
        {
            public float x;
            public float y;
        };
        public axis[] point = new axis[500]; // array of point for adding
        public int current_pos = 0; // recent and the number of point added
        public axis CENTRAL_POINT; // central

        public float Radius_CIRCLE; // radius for circle
        public float RANGE_CIRCLE = 50; // acceptable point for circle
        public float PERCENTAGE_CIRCLE; // pecentage of the number of true points
        public int NUMBER_CIRCLE_POINT = 0; // Number of True point of CIRCLE

        public float RANGE_VERTICAL = 15; // range for vertical line
        public int NUMBER_VERTICAL_POINT = 0; // Number of True point of Vertical 
        public float PERCENTAGE_VERTICAL = 0; // 

        public float RANGE_HORIZONTAL = 15; // range for vertical line
        public int NUMBER_HORIZONTAL_POINT = 0; // Number of True point of Vertical 
        public float PERCENTAGE_HORIZONTAL = 0; // Percentage

        public float temp;
        public int getCurrentPos()
        {
            return current_pos;
        }
        /*
        *   add a new point to array axis
        *   
        */
        public void add(axis p)
        {
            current_pos++;
            point[current_pos].x = p.x;
            point[current_pos].y = p.y;
        }
        public void setCENTER(axis p)
        {
            CENTRAL_POINT.x = p.x;
            CENTRAL_POINT.y = p.y;
            point[0].x = p.x;
            point[0].y = p.y;
        }
        public void setRADIUS(float radius)
        {
            Radius_CIRCLE = radius;
        }
        public void setRANGE_CIRCLE(float Ra)
        {
            RANGE_CIRCLE = Ra;
        }
        /*
        *   Check the End Point 
        *   True if inside the radius range of the 1st point
        *   false if outside the radius
        */
        public Boolean Check_End_Point()
        {
            float a = (point[current_pos].x - point[0].x) * (point[current_pos].x - point[0].x),
                  b = (point[current_pos].y - point[0].y) * (point[current_pos].y - point[0].y);
            float kc = (float)(Math.Sqrt(a + b));
            if ((kc > 0) && (kc < RANGE_CIRCLE))
            {
                return true;
            }
            else return false;
        }
        /*
        *   Check current point (current_pos) is "true" of Circle
        *   Using distance near the line 
        */
        public Boolean Check_Circle()
        {
            float a = (float)Math.Pow(point[current_pos - 1].x - CENTRAL_POINT.x, 2);
            float b = (float)Math.Pow(point[current_pos - 1].y - CENTRAL_POINT.y, 2);

            if ((Math.Sqrt(a + b) > Radius_CIRCLE - RANGE_CIRCLE) && (Math.Sqrt(a + b) < Radius_CIRCLE + RANGE_CIRCLE))
            {
                NUMBER_CIRCLE_POINT++;
                PERCENTAGE_CIRCLE = (NUMBER_CIRCLE_POINT / current_pos) * 100;
                return true;
            }
            else return false;
        }
        /*
        *   Check current point (current_pos) is "true" of Circle
        *   
        */
        public Boolean Check_Triangle()
        {
            return false;
        }
        public Boolean Check_Square()
        {
            return false;
        }
        /*
        *   Check current point in vertical of the 1st point
        */
        public Boolean Check_Vertical()
        {
            if ((point[current_pos].x > point[0].x - RANGE_VERTICAL) && (point[current_pos].x < point[0].x + RANGE_VERTICAL))
            {
                NUMBER_VERTICAL_POINT++;
                PERCENTAGE_VERTICAL = ((NUMBER_VERTICAL_POINT / current_pos) * (current_pos / 500)) * 100;
                return true;
            }
            else
            {
                return false;
            }
        }
        public Boolean Check_Horizontal()
        {
            if ((point[current_pos].y > point[0].y - RANGE_HORIZONTAL) && (point[current_pos].y < point[0].y + RANGE_HORIZONTAL))
            {
                NUMBER_HORIZONTAL_POINT++;
                PERCENTAGE_HORIZONTAL = ((NUMBER_HORIZONTAL_POINT / current_pos) * (current_pos / 500)) * 100;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
