//
// ExtensionsForGtkAction.cs
//
// Authors:
//   Pete Johanson <peter@peterjohanson.com>
//
// Copyright (C) 2010 Pete Johanson
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
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text.RegularExpressions;

using Gtk;
using Windows7Support;

namespace Banshee.Windows
{
    // TODO: If Gtk# ever supports implementing GtkActivatable interface,
    //       we could use that for updating the ThumbnailtToolbarButton 'automatically'
    public static class ExtensionsForGtkAction
    {
        static short id = 1;

        public static ThumbnailToolbarButton CreateThumbnailToolbarButton (this Gtk.Action action)
        {
            return action.CreateThumbnailToolbarButton (null);
        }

        public static ThumbnailToolbarButton CreateThumbnailToolbarButton (this Gtk.Action action,
            Func<Gtk.Action, System.Drawing.Icon> icon_callback)
        {
            var button = new ThumbnailToolbarButton (id++) {
                DismissOnClick = false,
                Tooltip = action.Label.Replace ("_", ""),
                Enabled = action.Sensitive,
                Hidden = !action.Visible,
                Icon = icon_callback != null ? icon_callback (action) : null
            };

            button.Clicked += (o, e) => action.Activate ();

            action.AddNotification ("icon-name", (o, args) => button.Icon = icon_callback != null ? icon_callback (action) : null);
            action.AddNotification ("stock-id", (o, args) => button.Icon = icon_callback != null ? icon_callback (action) : null);
            action.AddNotification ("tooltip", (o, args) => button.Tooltip = action.Label.Replace ("_", ""));
            action.AddNotification ("sensitive", (o, args) => button.Enabled = action.Sensitive);
            action.AddNotification ("visible", (o, args) => button.Hidden = !action.Visible);

            return button;
        }
    }
}
