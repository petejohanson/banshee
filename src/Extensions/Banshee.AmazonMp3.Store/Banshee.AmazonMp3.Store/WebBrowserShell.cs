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
using Mono.Unix;

using Banshee.WebBrowser;

namespace Banshee.AmazonMp3.Store
{
    public class WebBrowserShell : Table, Banshee.Gui.IDisableKeybindings
    {
        private ScrolledWindow store_view_scroll = new ScrolledWindow ();
        private StoreView store_view = new StoreView ();
        private NavigationControl navigation_control = new NavigationControl ();
        private Label title = new Label ();

        public WebBrowserShell () : base (2, 1, false)
        {
            store_view.LoadStatusChanged += (o, e) => {
                if (store_view.LoadStatus == OssiferLoadStatus.FirstVisuallyNonEmptyLayout) {
                    UpdateTitle (store_view.Title);
                }
            };

            navigation_control.WebView = store_view;
            navigation_control.GoHomeEvent += (o, e) => store_view.GoHome ();

            Attach (navigation_control, 0, 1, 0, 1,
                AttachOptions.Shrink,
                AttachOptions.Shrink,
                0, 0);

            title.Xalign = 0.0f;
            title.Xpad = 10;
            title.Ellipsize = Pango.EllipsizeMode.End;

            Attach (title, 1, 2, 0, 1,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Shrink,
                0, 0);

            store_view_scroll.Add (store_view);
            store_view_scroll.ShadowType = ShadowType.In;

            Attach (store_view_scroll, 0, 2, 1, 2,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Expand | AttachOptions.Fill,
                0, 0);

            UpdateTitle (Catalog.GetString ("Loading Amazon MP3 Store..."));

            ShowAll ();
        }

        private void UpdateTitle (string titleText)
        {
            if (store_view.Uri != "about:blank") {
                if (String.IsNullOrEmpty (titleText)) {
                    titleText = Catalog.GetString ("Amazon MP3 Store");
                }
                title.Markup = String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (titleText));
            }
        }
    }
}