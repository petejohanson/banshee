// 
// NavigationControl.cs
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
using Hyena.Gui;

namespace Banshee.WebBrowser
{
    public class NavigationControl : HBox
    {
        private Button back_button = new Button (new Image (Stock.GoBack, IconSize.Button)) { Relief = ReliefStyle.None };
        private Button forward_button = new Button (new Image (Stock.GoForward, IconSize.Button)) { Relief = ReliefStyle.None };
        private Button reload_button = new Button (new Image (Stock.Refresh, IconSize.Button)) { Relief = ReliefStyle.None };
        private Button home_button = new Button (new Image (Stock.Home, IconSize.Button)) { Relief = ReliefStyle.None };

        public event EventHandler GoHomeEvent;

        public NavigationControl ()
        {
            back_button.Clicked += (o, e) => {
                if (web_view != null && web_view.CanGoBack) {
                    web_view.GoBack ();
                }
            };

            forward_button.Clicked += (o, e) => {
                if (web_view != null && web_view.CanGoForward) {
                    web_view.GoForward ();
                }
            };

            reload_button.Clicked += (o, e) => {
                if (web_view != null) {
                    web_view.Reload (!GtkUtilities.NoImportantModifiersAreSet ());
                }
            };

            home_button.Clicked += (o, e) => {
                var handler = GoHomeEvent;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            };

            UpdateNavigation ();

            PackStart (back_button, false, false, 0);
            PackStart (forward_button, false, false, 0);
            PackStart (reload_button, false, false, 5);
            PackStart (home_button, false, false, 0);

            ShowAll ();
        }

        private OssiferWebView web_view;
        public OssiferWebView WebView {
            get { return web_view; }
            set {
                if (web_view == value) {
                    return;
                } else if (web_view != null) {
                    web_view.LoadStatusChanged -= OnOssiferWebViewLoadStatusChanged;
                }

                web_view = value;

                if (web_view != null) {
                    web_view.LoadStatusChanged += OnOssiferWebViewLoadStatusChanged;
                }

                UpdateNavigation ();
            }
        }

        public void UpdateNavigation ()
        {
            if (web_view == null) {
                Sensitive = false;
                return;
            }

            Sensitive = true;

            back_button.Sensitive = web_view.CanGoBack;
            forward_button.Sensitive = web_view.CanGoForward;
        }

        private void OnOssiferWebViewLoadStatusChanged (object o, EventArgs args)
        {
            if (web_view.LoadStatus == OssiferLoadStatus.Committed ||
                web_view.LoadStatus == OssiferLoadStatus.Failed) {
                UpdateNavigation ();
            }
        }
    }
}
