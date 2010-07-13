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

using Banshee.Widgets;
using Banshee.WebBrowser;

namespace Banshee.AmazonMp3.Store
{
    public class WebBrowserShell : Table, Banshee.Gui.IDisableKeybindings
    {
        private ScrolledWindow store_view_scroll = new ScrolledWindow ();
        private StoreView store_view = new StoreView ();
        private NavigationControl navigation_control = new NavigationControl ();
        private Label title = new Label ();
        private Button sign_out_button = new Button (Catalog.GetString ("Sign out of Amazon")) { Relief = ReliefStyle.None };
        private SearchEntry search_entry = new SearchEntry ();
        private int search_clear_on_navigate_state;

        public WebBrowserShell () : base (2, 4, false)
        {
            RowSpacing = 5;

            store_view.LoadStatusChanged += (o, e) => {
                if (store_view.LoadStatus == OssiferLoadStatus.FirstVisuallyNonEmptyLayout) {
                    UpdateTitle (store_view.Title);

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

            store_view.Ready += (o, e) => navigation_control.WebView = store_view;

            navigation_control.GoHomeEvent += (o, e) => store_view.GoHome ();

            Attach (navigation_control, 0, 1, 0, 1,
                AttachOptions.Shrink,
                AttachOptions.Shrink,
                0, 0);

            title.Xalign = 0.0f;
            title.Xpad = 20;
            title.Ellipsize = Pango.EllipsizeMode.End;

            Attach (title, 1, 2, 0, 1,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Shrink,
                0, 0);

            sign_out_button.Clicked += (o, e) => store_view.SignOut ();

            Attach (sign_out_button, 2, 3, 0, 1,
                AttachOptions.Shrink,
                AttachOptions.Shrink,
                0, 0);

            search_entry.EmptyMessage = String.Format (Catalog.GetString ("Search the Amazon MP3 Store"));
            search_entry.SetSizeRequest (260, -1);
            // FIXME: dummy option to make the "search" icon show up;
            // we should probably fix this in the SearchEntry, but also
            // add real filter options for searching Amazon MP3 (artist,
            // album, genre, etc.)
            search_entry.AddFilterOption (0, Catalog.GetString ("Amazon MP3 Store"));
            search_entry.Show ();
            search_entry.Activated += (o, e) => {
                store_view.GoSearch (search_entry.Query);
                store_view.HasFocus = true;
                search_clear_on_navigate_state = 1;
            };
            Attach (search_entry, 3, 4, 0, 1,
                AttachOptions.Fill,
                AttachOptions.Shrink,
                0, 0);

            store_view_scroll.Add (store_view);
            store_view_scroll.ShadowType = ShadowType.In;

            Attach (store_view_scroll, 0, 4, 1, 2,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Expand | AttachOptions.Fill,
                0, 0);

            UpdateTitle (Catalog.GetString ("Loading Amazon MP3 Store..."));

            ShowAll ();

            store_view.SignInChanged += (o, e) => UpdateSignInButton ();
            UpdateSignInButton ();
        }

        private void UpdateSignInButton ()
        {
            sign_out_button.Visible = store_view.IsSignedIn;
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
