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

using SlimDX;
using System.Drawing;
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes
{


	
	public class SubclassHWND : NativeWindow
   {
   		public SubclassHWND()
   		{
   			Touchdown += OnTouchDownHandler;
            Touchup += OnTouchUpHandler;
            TouchMove += OnTouchMoveHandler;
   		}
   	
		public ILogger FLogger;
   		public Dictionary<int, TouchData> State = new Dictionary<int, TouchData>();
   	
   		[DllImport("user32.dll")]
		static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);
   
	   	
	   	protected override void WndProc(ref Message m)
	    {
	    	bool handled;
            switch (m.Msg)
            {
                case TouchConstants.WM_TOUCH:
                    handled = DecodeTouch(ref m);
                    break;
                default:
                    handled = false;
                    break;
            }
            base.WndProc(ref m);  // Call parent WndProc for default message processing.

            if (handled) // Acknowledge event if handled.
                m.Result = new System.IntPtr(1);
	    	/*if (m.Msg == TouchConstants.WM_TOUCH)
	    	{
				FLogger.Log(LogType.Debug,m.ToString());
	    	} 
	    	
		     base.WndProc(ref m);*/
	    }
   	
   	    private bool DecodeTouch(ref Message m)
        {
            // More than one touchinput may be associated with a touch message,
            int inputCount = (m.WParam.ToInt32() & 0xffff); // Number of touch inputs, actual per-contact messages
            TOUCHINPUT[] inputs = new TOUCHINPUT[inputCount];

            if (!TouchConstants.GetTouchInputInfo(m.LParam, inputCount, inputs, Marshal.SizeOf(new TOUCHINPUT())))
            {
                return false;
            }

            bool handled = false;
            for (int i = 0; i < inputCount; i++)
            {
                TOUCHINPUT ti = inputs[i];

                EventHandler<WMTouchEventArgs> handler = null;
                if ((ti.dwFlags & TouchConstants.TOUCHEVENTF_DOWN) != 0)
                {
                    handler = Touchdown;
                }
                else if ((ti.dwFlags & TouchConstants.TOUCHEVENTF_UP) != 0)
                {
                    handler = Touchup;
                }
                else if ((ti.dwFlags & TouchConstants.TOUCHEVENTF_MOVE) != 0)
                {
                    handler = TouchMove;
                }

                // Convert message parameters into touch event arguments and handle the event.
                if (handler != null)
                {
                    WMTouchEventArgs te = new WMTouchEventArgs();

                    // TOUCHINFO point coordinates and contact size is in 1/100 of a pixel; convert it to pixels.
                    // Also convert screen to client coordinates.
                    te.ContactY = ti.cyContact / 100;
                    te.ContactX = ti.cxContact / 100;
                    te.Id = ti.dwID;
                    {
                    	Point pt = new Point(ti.x / 100, ti.y / 100);
                    	ScreenToClient(this.Handle,ref pt);
                        
                    	//Point pt = PointToClient();
                        te.LocationX = pt.X;
                        te.LocationY = pt.Y;
                    }
                    te.Time = ti.dwTime;
                    te.Mask = ti.dwMask;
                    te.Flags = ti.dwFlags;

                    handler(this, te);

                    // Mark this event as handled.
                    handled = true;
                }
            }
            TouchConstants.CloseTouchInputHandle(m.LParam);

            return handled;
        }
   	
   	        private event EventHandler<WMTouchEventArgs> Touchdown;
        private event EventHandler<WMTouchEventArgs> Touchup;
        private event EventHandler<WMTouchEventArgs> TouchMove;

        private void OnTouchDownHandler(object sender, WMTouchEventArgs e)
        {

                TouchData t = new TouchData();
                t.Id = e.Id;
                t.IsNew = true;
                t.Pos = new Vector2(e.LocationX, e.LocationY);
                this.State.Add(e.Id, t);
            
        }

        private void OnTouchUpHandler(object sender, WMTouchEventArgs e)
        {

                this.State.Remove(e.Id);
            
        }

        private void OnTouchMoveHandler(object sender, WMTouchEventArgs e)
        {
                TouchData t = this.State[e.Id];
                t.Pos = new Vector2(e.LocationX, e.LocationY);
            
        }
   }
   
	
	
	#region PluginInfo
	[PluginInfo(Name = "WindowsTouch", Category = "Windows", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class WindowsTestHookNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Handle", DefaultValue = 1.0)]
		public IDiffSpread<int> FInput;
		
		[Input("PalmRejection")]
		public IDiffSpread<bool> FMode;

		[Output("Id")]
		public ISpread<int> FOutId;
		
		[Output("Position")]
		public ISpread<Vector2> FOutPos;
		
		
		[Output("Is New")]
		public ISpread<bool> FOutNew;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins
		
		private SubclassHWND src;
		
		public Dictionary<int, TouchData> State = new Dictionary<int, TouchData>();
		
		[DllImport("user32.dll")]
		static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);
   
 
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FInput.IsChanged || FMode.IsChanged)
			{ 
				if (src != null)
				{
					src.ReleaseHandle();
				}
				IntPtr wnd = new IntPtr(this.FInput[0]);
				ulong mode = 2;
				//Palm or Finger
				if(FMode[0]==true)mode=0; else mode=2;
				TouchConstants.RegisterTouchWindow(wnd,mode);
				src = new SubclassHWND();
				src.State = this.State;
				src.AssignHandle(wnd);
				src.FLogger = this.FLogger;
			}
			
			this.FOutId.SliceCount = this.State.Count;
			this.FOutPos.SliceCount = this.State.Count;
			this.FOutNew.SliceCount= this.State.Count;
			
			Rectangle r;
			
			GetClientRect(this.src.Handle,out r);
			
			
			float fw = (float)r.Width;
            float fh = (float)r.Height;
			
			
			
			int cnt = 0;
			foreach (int k in this.State.Keys)
			{
				TouchData t = this.State[k].Clone(fw,fh);
				
				this.FOutId[cnt] = t.Id;
				this.FOutPos[cnt] = t.Pos;
				this.FOutNew[cnt] = t.IsNew;
				cnt++;
			}
		}

	}
}
