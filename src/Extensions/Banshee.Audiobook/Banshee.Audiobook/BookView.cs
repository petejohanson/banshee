//
// BookView.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
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

using Mono.Unix;
using Gtk;

using Hyena;
using Hyena.Collections;
using Hyena.Data.Sqlite;

using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Widgets;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.Collection.Database;
using Banshee.Configuration;
using Banshee.Database;
using Banshee.Gui;
using Banshee.Gui.Widgets;
using Banshee.Library;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.Playlist;
using Banshee.Preferences;
using Banshee.ServiceStack;
using Banshee.Sources;
using Hyena.Gui.Theming;

namespace Banshee.Audiobook
{
    public class BookView : Hyena.Widgets.RoundedFrame, Banshee.Sources.Gui.ISourceContents, IDisposable
    {
        AudiobookLibrarySource library;

        Alignment align;
        Label title;
        BookCover cover;
        BaseTrackListView track_list;

        public BookView ()
        {
            BuildInfoBox ();
            //BuildFilesBox ();

            ShowAll ();
        }

        public void SetBook (DatabaseAlbumInfo book)
        {
            ThreadAssist.AssertInMainThread ();

            title.Markup = String.Format (
                "<span size=\"x-large\" weight=\"bold\">{0}</span>\n" +
                "{1}",
                GLib.Markup.EscapeText (book.Title),
                GLib.Markup.EscapeText (book.ArtistName)
            );

            cover.LoadImage (
                TrackMediaAttributes.AudioStream | TrackMediaAttributes.AudioBook,
                CoverArtSpec.CreateArtistAlbumId (book.ArtistName, book.Title)
            );
        }

        public override void Dispose ()
        {
            if (cover != null) {
                cover.Dispose ();
                cover = null;
            }

            base.Dispose ();
        }

#region ISourceContents

        public bool SetSource (ISource source)
        {
            library = source as AudiobookLibrarySource;

            if (library != null) {
                track_list.SetModel (library.TrackModel);
            }

            return library != null;
        }

        public void ResetSource ()
        {
        }

        public ISource Source { get { return library; } }

        public Widget Widget { get { return this; } }

#endregion

        private void BuildInfoBox ()
        {
            align = new Alignment (0.5f, 0.0f, 0.0f, 0.0f) { BorderWidth = 6 };
            align.TopPadding = 6;

            var hbox = new HBox () { Spacing = 12 };

            // Left box - cover art, title, etc
            var left_box = new VBox () { Spacing = 12 };

            // Cover art
            cover = new BookCover (this) {
                WidthRequest = 300,
                HeightRequest = 300
            };
            var editable_cover = TrackInfoDisplay.GetEditable (cover);

            // Title
            title = new Label () {
                Xalign = 0,
                Yalign = 0
            };

            // Packing
            left_box.PackStart (editable_cover, false, false,  0);
            left_box.PackStart (title, true, true,  0);

            hbox.PackStart (left_box, false, false, 0);

            // Right box - bookmarks, track list
            var right_box = new VBox () { Spacing = 12 };

            var notebook = new Notebook () { WidthRequest = 450, HeightRequest = 600 };
            notebook.ShowBorder = false;
            notebook.AppendPage (new Label ("Bookmark support coming soon!"), new Label (Catalog.GetString ("Bookmarks")));

            // Tracks

            track_list = new BaseTrackListView () {
                HeaderVisible = true,
                IsEverReorderable = false
            };

            var columns = new DefaultColumnController ();
            var file_columns = new ColumnController ();
            file_columns.AddRange (
                columns.IndicatorColumn,
                columns.DiscNumberAndCountColumn,
                columns.TitleColumn,
                columns.DurationColumn
            );
            file_columns.SortColumn = columns.DiscNumberAndCountColumn;

            var track_sw = new Gtk.ScrolledWindow ();
            track_sw.Child = track_list;

            foreach (var col in file_columns) {
                col.Visible = true;
            }

            track_list.ColumnController = file_columns;

            notebook.AppendPage (track_sw, new Label (Catalog.GetString ("Tracks")));


            right_box.PackStart (notebook, false, false, 0);

            hbox.PackStart (right_box, false, false, 0);

            align.Child = hbox;
            Child = align;
            ShowAll ();
        }
    }
}
