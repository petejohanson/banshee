//
// BookCover.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (c) 2010 Novell, Inc.
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

using Hyena.Gui.Theming;

using Banshee.Gui.Widgets;
using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.Collection.Gui;
using Banshee.Gui;

namespace Banshee.Audiobook
{
    public class BookCover : Gtk.Image
    {
        public BookCover (Widget parent)
        {
        }

        public void LoadImage (TrackMediaAttributes attr, string artwork_id)
        {
            if (Pixbuf != null) {
                Pixbuf.Dispose ();
            }

            Pixbuf = ServiceManager.Get<ArtworkManager> ().LookupScalePixbuf (artwork_id, HeightRequest)
                ?? IconThemeUtils.LoadIcon ("audiobook", HeightRequest);
        }
    }
}