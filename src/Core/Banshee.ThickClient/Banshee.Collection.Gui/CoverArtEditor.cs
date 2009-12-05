//
// CoverArtEditor.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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

using Mono.Unix;
using Mono.Addins;
using Gtk;

using Hyena.Gui;
using Hyena.Widgets;

using Banshee.Base;
using Banshee.Kernel;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Configuration.Schema;

using Banshee.Widgets;
using Banshee.Gui.DragDrop;

namespace Banshee.Collection.Gui
{
    public class CoverArtEditor
    {
        public static Widget For (Widget widget, Func<int, int, bool> is_sensitive, Func<TrackInfo> get_track, System.Action on_updated)
        {
            var box = new EventBox ();
            box.Child = widget;

            Gtk.Drag.SourceSet (box, Gdk.ModifierType.Button1Mask, new TargetEntry [] { DragDropTarget.UriList }, Gdk.DragAction.Copy | Gdk.DragAction.Move);
            box.DragDataGet += (o, a) => {
                var uri = GetCoverArtPath (get_track ());
                if (uri != null) {
                    a.SelectionData.Set (Gdk.Atom.Intern (DragDropTarget.UriList.Target, false), 8, System.Text.Encoding.UTF8.GetBytes (uri));
                    a.RetVal = true;
                }
            };

            return box;
        }

        private static string GetCoverArtPath (TrackInfo track)
        {
            if (track != null) {
                var uri = new SafeUri (CoverArtSpec.GetPath (track.ArtworkId));
                if (Banshee.IO.File.Exists (uri)) {
                    return uri.AbsoluteUri;
                }
            }

            return null;
        }
    }
}
