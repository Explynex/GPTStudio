using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GPTStudio.Utils
{
    class Win32
    {

        #region Imports

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(ref Point pt);

        #endregion

        #region Native handlers
        public static Point GetMousePosition()
        {
            var w32Mouse = new Point();
            GetCursorPos(ref w32Mouse);

            return w32Mouse;
        } 
        #endregion
    }
}
