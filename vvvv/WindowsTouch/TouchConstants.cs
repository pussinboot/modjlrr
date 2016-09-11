#region usings
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using System.Windows.Interop;
using System.Windows.Forms;
using System.Collections.Generic;

using SlimDX;

#endregion usings

namespace VVVV.Nodes
{	
	[StructLayout(LayoutKind.Sequential)]
    public struct TOUCHINPUT
    {
        public int x;
        public int y;
        public System.IntPtr hSource;
        public int dwID;
        public int dwFlags;
        public int dwMask;
        public int dwTime;
        public System.IntPtr dwExtraInfo;
        public int cxContact;
        public int cyContact;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTS
    {
        public short x;
        public short y;
    }
	
	public class TouchConstants
    {
        public const int WM_TOUCH = 0x0240;

        // Touch event flags ((TOUCHINPUT.dwFlags) [winuser.h]
        public const int TOUCHEVENTF_MOVE = 0x0001;
        public const int TOUCHEVENTF_DOWN = 0x0002;
        public const int TOUCHEVENTF_UP = 0x0004;
        public const int TOUCHEVENTF_INRANGE = 0x0008;
        public const int TOUCHEVENTF_PRIMARY = 0x0010;
        public const int TOUCHEVENTF_NOCOALESCE = 0x0020;
        public const int TOUCHEVENTF_PEN = 0x0040;

        // Touch input mask values (TOUCHINPUT.dwMask) [winuser.h]
        public const int TOUCHINPUTMASKF_TIMEFROMSYSTEM = 0x0001; // the dwTime field contains a system generated value
        public const int TOUCHINPUTMASKF_EXTRAINFO = 0x0002; // the dwExtraInfo field is valid
        public const int TOUCHINPUTMASKF_CONTACTAREA = 0x0004; // the cxContact and cyContact fields are valid


        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterTouchWindow(System.IntPtr hWnd, ulong ulFlags);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetTouchInputInfo(System.IntPtr hTouchInput, int cInputs, [In, Out] TOUCHINPUT[] pInputs, int cbSize);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern void CloseTouchInputHandle(System.IntPtr lParam);
    }
	
	public class TouchData
    {
        public int Id;
        public Vector2 Pos;
        public bool IsNew;

        public TouchData Clone()
        {
            TouchData t = new TouchData();
            t.Id = this.Id;
            t.Pos = this.Pos;
            t.IsNew = this.IsNew;

            return t;
        }

        public TouchData Clone(float sizex,float sizey)
        {
            TouchData t = new TouchData();

            t.Id = this.Id;

            float x = (float)VMath.Map(this.Pos.X, 0, sizex, -1.0f, 1.0f, TMapMode.Float);
            float y = (float)VMath.Map(this.Pos.Y, 0, sizey, 1.0f, -1.0f, TMapMode.Float);

            t.Pos = new Vector2(x, y);
            t.IsNew = this.IsNew;

            return t;
        }
    }
	



    public class WMTouchEventArgs : System.EventArgs
    {
        // Private data members
        private int x;                  // touch x client coordinate in pixels
        private int y;                  // touch y client coordinate in pixels
        private int id;                 // contact ID
        private int mask;               // mask which fields in the structure are valid
        private int flags;              // flags
        private int time;               // touch event time
        private int contactX;           // x size of the contact area in pixels
        private int contactY;           // y size of the contact area in pixels

        // Access to data members
        public int LocationX
        {
            get { return x; }
            set { x = value; }
        }
        public int LocationY
        {
            get { return y; }
            set { y = value; }
        }
        public int Id
        {
            get { return id; }
            set { id = value; }
        }
        public int Flags
        {
            get { return flags; }
            set { flags = value; }
        }
        public int Mask
        {
            get { return mask; }
            set { mask = value; }
        }
        public int Time
        {
            get { return time; }
            set { time = value; }
        }
        public int ContactX
        {
            get { return contactX; }
            set { contactX = value; }
        }
        public int ContactY
        {
            get { return contactY; }
            set { contactY = value; }
        }
        public bool IsPrimaryContact
        {
            get { return (flags & TouchConstants.TOUCHEVENTF_PRIMARY) != 0; }
        }

        // Constructor
        public WMTouchEventArgs()
        {
        }
    }
}
