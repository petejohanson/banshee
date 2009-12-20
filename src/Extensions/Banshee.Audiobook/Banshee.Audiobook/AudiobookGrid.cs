//
// AudiobookGrid.cs
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
using Gtk;

using Hyena;
using Hyena.Data;
using Hyena.Data.Gui;

using Banshee.Widgets;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Collection.Gui;
using Banshee.Gui;
using Banshee.Gui.Widgets;
using Banshee.Sources.Gui;
using Banshee.Web;

namespace Banshee.Audiobook
{
    public class AudiobookGrid : SearchableListView<AlbumInfo>
    {
        private ColumnCellAlbum renderer;

        public AudiobookGrid ()
        {
            renderer = new ColumnCellAlbum () {
                LayoutStyle = DataViewLayoutStyle.Grid,
                ImageSize = 150
            };
            var column_controller = new ColumnController ();
            column_controller.Add (new Column ("Album", renderer, 1.0));

            LayoutStyle = DataViewLayoutStyle.Grid;
            ColumnController = column_controller;
            //RowActivated += OnRowActivated;
        }

        public override bool SelectOnRowFound {
            get { return true; }
        }

        protected override Gdk.Size OnMeasureChild ()
        {
            return renderer.Measure (this);
        }

        protected override bool OnPopupMenu ()
        {
            //ServiceManager.Get<InterfaceActionService> ().TrackActions["TrackContextMenuAction"].Activate ();
            return true;
        }
    }
}
