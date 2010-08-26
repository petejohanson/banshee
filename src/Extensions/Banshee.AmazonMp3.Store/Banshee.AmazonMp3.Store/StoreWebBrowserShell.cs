//
// StoreWebBrowserShell.cs
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

namespace Banshee.AmazonMp3.Store
{
    public class StoreWebBrowserShell : Banshee.WebSource.WebBrowserShell
    {
        public StoreView StoreView { get; private set; }

        public StoreWebBrowserShell (StoreView store_view) : base (Catalog.GetString ("Amazon MP3 Store"), store_view)
        {
            StoreView = store_view;

            SearchEntry.EmptyMessage = String.Format (Catalog.GetString ("Search the Amazon MP3 Store"));

            NavigationControl.AddLink (Catalog.GetString ("How Your Purchases Support GNOME"), StoreView.GetActionUrl ("about/"));

            var signout = NavigationControl.AddLink ("Sign out of Amazon", null);
            store_view.SignInChanged += (o, a) => signout.Visible = store_view.IsSignedIn;
            signout.Activated += (o, a) => store_view.SignOut ();
            signout.Visible = store_view.IsSignedIn;

            ShowAll ();
        }
    }
}
