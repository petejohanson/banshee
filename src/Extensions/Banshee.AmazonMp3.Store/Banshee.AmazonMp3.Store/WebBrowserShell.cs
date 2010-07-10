//
// WebBrowserShell.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2010 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

using Gtk;

using Banshee.WebBrowser;

namespace Banshee.AmazonMp3.Store
{
    public class WebBrowserShell : Table, Banshee.Gui.IDisableKeybindings
    {
        private ScrolledWindow store_view_scroll = new ScrolledWindow ();
        private StoreView store_view = new StoreView ();
        private NavigationControl navigation_control = new NavigationControl ();

        public WebBrowserShell () : base (2, 1, false)
        {
            navigation_control.WebView = store_view;

            Attach (navigation_control, 0, 1, 0, 1,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Shrink,
                0, 0);

            store_view_scroll.Add (store_view);
            store_view_scroll.ShadowType = ShadowType.In;

            Attach (store_view_scroll, 0, 1, 1, 2,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Expand | AttachOptions.Fill,
                0, 0);

            ShowAll ();
        }
    }
}