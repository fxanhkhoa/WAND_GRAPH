using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WAND_GRAPH
{
    class AdvancedCursors
    {
        [DllImport("User32.dll")]
        private static extern IntPtr LoadCursorFromFile(String str);

        public static Cursor Create(string filename)
        {
            IntPtr hCursor = LoadCursorFromFile(filename);

            if (!IntPtr.Zero.Equals(hCursor))
            {
                return new Cursor(hCursor);
            }
            else
            {
                throw new ApplicationException("Could not create cursor from file " + filename);
            }
        }
    }
}
