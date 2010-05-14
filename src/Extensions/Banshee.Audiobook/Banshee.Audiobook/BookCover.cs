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

using Banshee.Gui.Widgets;
using Banshee.Collection;
using Hyena.Gui.Theming;

namespace Banshee.Audiobook
{
    public class BookCover : CoverArtDisplay
    {
        Widget parent;

        public BookCover (Widget parent)
        {
            this.parent = parent;
            MissingVideoIconName = MissingAudioIconName = "audiobook";
        }

        protected override void OnThemeChanged ()
        {
            base.OnThemeChanged ();

            var theme = Hyena.Gui.Theming.ThemeEngine.CreateTheme (parent);
            BackgroundColor = theme.Colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal);
        }

        public void LoadImage (TrackMediaAttributes attr, string artwork_id)
        {
             LoadImage (attr, artwork_id, true);
        }
    }
}