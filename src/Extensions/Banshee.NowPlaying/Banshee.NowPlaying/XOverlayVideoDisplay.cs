//
// XOverlayVideoDisplay.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gtk;

using Banshee.ServiceStack;

namespace Banshee.NowPlaying
{   
    public class XOverlayVideoDisplay : VideoDisplay
    {
        private Gdk.Window video_window;                
        protected override Gdk.Window RenderWindow {
            get { return video_window; }
        }

        public XOverlayVideoDisplay () : base ()
        {
        }
        
        protected override void OnRealized ()
        {
            WidgetFlags |= WidgetFlags.Realized;
        
            Gdk.WindowAttr attributes = new Gdk.WindowAttr ();
            attributes.WindowType = Gdk.WindowType.Child;
            attributes.X = Allocation.X;
            attributes.Y = Allocation.Y;
            attributes.Width = Allocation.Width;
            attributes.Height = Allocation.Height;
            attributes.Visual = Visual;
            attributes.Wclass = Gdk.WindowClass.InputOutput;
            attributes.Colormap = Colormap;
            attributes.EventMask = (int)(Gdk.EventMask.ExposureMask | Gdk.EventMask.VisibilityNotifyMask);
            
            Gdk.WindowAttributesType attributes_mask = 
                Gdk.WindowAttributesType.X | 
                Gdk.WindowAttributesType.Y | 
                Gdk.WindowAttributesType.Visual | 
                Gdk.WindowAttributesType.Colormap;
                
            GdkWindow = new Gdk.Window (Parent.GdkWindow, attributes, attributes_mask);
            GdkWindow.UserData = Handle;
            
            if (video_window != null) {
                video_window.Reparent (GdkWindow, 0, 0);
                video_window.MoveResize (0, 0, Allocation.Width, Allocation.Height);
                video_window.ShowUnraised ();
                return;
            }
            
            attributes = new Gdk.WindowAttr ();
            attributes.WindowType = Gdk.WindowType.Child;
            attributes.X = 0;
            attributes.Y = 0;
            attributes.Width = Allocation.Width;
            attributes.Height = Allocation.Height;
            attributes.Visual = Visual;
            attributes.Wclass = Gdk.WindowClass.InputOutput;
            attributes.Colormap = Colormap;
            attributes.EventMask = (int)(Gdk.EventMask.ExposureMask | Gdk.EventMask.VisibilityNotifyMask);
            
            attributes_mask = 
                Gdk.WindowAttributesType.X | 
                Gdk.WindowAttributesType.Y | 
                Gdk.WindowAttributesType.Visual | 
                Gdk.WindowAttributesType.Colormap;
                
            video_window = new Gdk.Window (GdkWindow, attributes, attributes_mask);
            video_window.UserData = Handle;
                        
            video_window.SetBackPixmap (null, false);
            
            ServiceManager.PlayerEngine.VideoWindow = video_window.Handle;
        }
        
        protected override void OnUnrealized ()
        {
            video_window.Hide ();
            video_window.Reparent (null, 0, 0);
            
            base.OnUnrealized ();
        }

        protected override void OnMapped ()
        {
            WidgetFlags |= WidgetFlags.Mapped;
            
            GdkWindow.Show ();
            video_window.ShowUnraised ();
        }
        
        protected override void OnUnmapped ()
        {
            WidgetFlags &= ~WidgetFlags.Mapped;
            
            video_window.Hide ();
            GdkWindow.Hide ();
        }
        
        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            if (!IsRealized) {
                return;
            }
            
            Gdk.Rectangle rect = new Gdk.Rectangle (0, 0, allocation.Width, allocation.Height);
            video_window.MoveResize (rect);
            
            base.OnSizeAllocated (allocation);
            
            QueueDraw ();
        }
        
        protected override bool OnConfigureEvent (Gdk.EventConfigure evnt)
        {
            if (video_window != null && ServiceManager.PlayerEngine.SupportsVideo) {
                ServiceManager.PlayerEngine.VideoExpose (video_window.Handle, true);
            }
            
            return false;
        }
        
        protected override void ExposeVideo (Gdk.EventExpose evnt)
        {
            ServiceManager.PlayerEngine.VideoExpose (video_window.Handle, true);
        }
    }
}