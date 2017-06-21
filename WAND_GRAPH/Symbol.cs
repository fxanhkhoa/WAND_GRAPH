using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAND_GRAPH
{
    public struct axis
    {
        public float x;
        public float y;
    };
    class Symbol
    {
        public axis[] point = new axis[100]; // array of point for adding
        public int current_pos = 0; // recent and the number of point added
        public axis CENTRAL_POINT; // central
        public float Radius_CIRCLE; // radius for circle
        public float RANGE_CIRCLE = 4; // acceptable point for circle
        public float PERCENTAGE_CIRCLE; // pecentage of the number of true points
        /*
        *   add a new point to array axis
        *   
        */
        public void add(axis p)
        {
            point[current_pos].x = p.x;
            point[current_pos].y = p.y;
            current_pos++;
        }
        public void setCENTER(axis p)
        {
            CENTRAL_POINT.x = p.x;
            CENTRAL_POINT.y = p.y;
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
        public Boolean Check_Circle()
        {

            return false;
        }
        public Boolean Check_Triangle()
        {
            return false;
        }
    }
}
