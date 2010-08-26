//
// WebBrowserShell.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
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

using Banshee.Widgets;
using Banshee.WebBrowser;

namespace Banshee.WebSource
{
    public class WebBrowserShell : Table, Banshee.Gui.IDisableKeybindings
    {
        private string name;
        private ScrolledWindow view_scroll = new ScrolledWindow ();
        private WebView view;
        private NavigationControl navigation_control = new NavigationControl ();
        private Label title = new Label ();
        private SearchEntry search_entry = new SearchEntry ();
        private int search_clear_on_navigate_state;

        public SearchEntry SearchEntry { get { return search_entry; } }

        public WebView View { get { return view; } }

        protected NavigationControl NavigationControl { get { return navigation_control; } }

        public WebBrowserShell (string name, WebView view) : base (2, 3, false)
        {
            this.name = name;
            this.view = view;

            RowSpacing = 5;

            view.LoadStatusChanged += (o, e) => {
                if (view.LoadStatus == OssiferLoadStatus.FirstVisuallyNonEmptyLayout) {
                    UpdateTitle (view.Title);

                    switch (search_clear_on_navigate_state) {
                        case 1:
                            search_clear_on_navigate_state = 2;
                            break;
                        case 2:
                            search_clear_on_navigate_state = 0;
                            search_entry.Query = String.Empty;
                            break;
                    }
                }
            };

            view.Ready += (o, e) => navigation_control.WebView = view;

            navigation_control.GoHomeEvent += (o, e) => view.GoHome ();

            Attach (navigation_control, 0, 1, 0, 1,
                AttachOptions.Shrink,
                AttachOptions.Shrink,
                0, 0);

            title.Xalign = 0.0f;
            title.Xpad = 6;
            title.Ellipsize = Pango.EllipsizeMode.End;

            Attach (title, 1, 2, 0, 1,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Shrink,
                0, 0);

            //search_entry.EmptyMessage = String.Format (Catalog.GetString ("Search the Amazon MP3 Store"));
            search_entry.SetSizeRequest (260, -1);
            // FIXME: dummy option to make the "search" icon show up;
            // we should probably fix this in the SearchEntry, but also
            // add real filter options for searching Amazon MP3 (artist,
            // album, genre, etc.)
            search_entry.AddFilterOption (0, name);
            search_entry.Show ();
            search_entry.Activated += (o, e) => {
                view.GoSearch (search_entry.Query);
                view.HasFocus = true;
                search_clear_on_navigate_state = 1;
            };
            Attach (search_entry, 2, 3, 0, 1,
                AttachOptions.Fill,
                AttachOptions.Shrink,
                0, 0);

            view_scroll.Add (view);
            view_scroll.ShadowType = ShadowType.In;

            Attach (view_scroll, 0, 3, 1, 2,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Expand | AttachOptions.Fill,
                0, 0);

            UpdateTitle (String.Format (Catalog.GetString ("Loading {0}..."), name));

            ShowAll ();
        }

        private void UpdateTitle (string titleText)
        {
            if (view.Uri != "about:blank") {
                if (String.IsNullOrEmpty (titleText)) {
                    titleText = name;
                }
                title.Markup = String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (titleText));
            }
        }
    }
}
