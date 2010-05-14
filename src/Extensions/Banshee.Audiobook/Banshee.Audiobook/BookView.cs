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
            notebook.AppendPage (new Label ("...my bookmarks..."), new Label ("Bookmarks"));

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

            notebook.AppendPage (track_sw, new Label ("Tracks"));


            right_box.PackStart (notebook, false, false, 0);

            hbox.PackStart (right_box, false, false, 0);

            //var vbox2 = new VBox ();
            //vbox2.PackStart (vbox, false, false, 0);

            //var sw = new Gtk.ScrolledWindow () { ShadowType = ShadowType.None };
            //sw.AddWithViewport (vbox2);
            //(sw.Child as Viewport).ShadowType = ShadowType.None;
            //frame.Child = sw;
            //frame.Child = vbox;
            //frame.ShowAll ();

            /*sw.Child.ModifyBg (StateType.Normal, Style.Base (StateType.Normal));
            sw.Child.ModifyFg (StateType.Normal, Style.Text (StateType.Normal));
            sw.Child.ModifyText (StateType.Normal, Style.Text (StateType.Normal));
            StyleSet += delegate {
                sw.Child.ModifyBg (StateType.Normal, Style.Base (StateType.Normal));
                sw.Child.ModifyFg (StateType.Normal, Style.Text (StateType.Normal));
                sw.Child.ModifyText (StateType.Normal, Style.Text (StateType.Normal));
            };*/

            //Add (frame);
            align.Child = hbox;
            Child = align;
            ShowAll ();
        }

        /*private void BuildFilesBox ()
        {
            var vbox = new VBox ();
            vbox.Spacing = 6;

            var file_list = new BaseTrackListView () {
                HeaderVisible = true,
                IsEverReorderable = false
            };

            var files_model = source.TrackModel as MemoryTrackListModel;
            var columns = new DefaultColumnController ();
            columns.TrackColumn.Title = "#";
            var file_columns = new ColumnController ();
            file_columns.AddRange (
                columns.IndicatorColumn,
                columns.TrackColumn,
                columns.TitleColumn,
                columns.DurationColumn,
                columns.FileSizeColumn
            );

            foreach (var col in file_columns) {
                col.Visible = true;
            }

            var file_sw = new Gtk.ScrolledWindow ();
            file_sw.Child = file_list;

            var tracks = new List<TrackInfo> ();

            var files = new List<IA.DetailsFile> (details.Files);

            string [] format_blacklist = new string [] { "metadata", "fingerprint", "checksums", "xml", "m3u", "dublin core", "unknown" };
            var formats = new List<string> ();
            foreach (var f in files) {
                var track = new TrackInfo () {
                    Uri         = new SafeUri (f.Location),
                    FileSize    = f.Size,
                    TrackNumber = f.Track,
                    ArtistName  = f.Creator ?? details.Creator,
                    AlbumTitle  = item.Title,
                    TrackTitle  = f.Title,
                    BitRate     = f.BitRate,
                    MimeType    = f.Format,
                    Duration    = f.Length
                };

                // Fix up duration/track#/title
                if ((f.Length == TimeSpan.Zero || f.Title == null || f.Track == 0) && !f.Location.Contains ("zip") && !f.Location.EndsWith ("m3u")) {
                    foreach (var b in files) {
                        if ((f.Title != null && f.Title == b.Title)
                                || (f.OriginalFile != null && b.Location != null && b.Location.EndsWith (f.OriginalFile))
                                || (f.OriginalFile != null && f.OriginalFile == b.OriginalFile)) {
                            if (track.Duration == TimeSpan.Zero)
                                track.Duration = b.Length;

                            if (track.TrackTitle == null)
                                track.TrackTitle = b.Title;

                            if (track.TrackNumber == 0)
                                track.TrackNumber = b.Track;

                            if (track.Duration != TimeSpan.Zero && track.TrackTitle != null && track.TrackNumber != 0)
                                break;
                        }
                    }
                }

                track.TrackTitle = track.TrackTitle ?? System.IO.Path.GetFileName (f.Location);

                tracks.Add (track);

                if (f.Format != null && !formats.Contains (f.Format)) {
                    if (!format_blacklist.Any (fmt => f.Format.ToLower ().Contains (fmt))) {
                        formats.Add (f.Format);
                    }
                }
            }

            // Order the formats according to the preferences
            string format_order = String.Format (", {0}, {1}, {2},", HomeSource.VideoTypes.Get (), HomeSource.AudioTypes.Get (), HomeSource.TextTypes.Get ()).ToLower ();

            var sorted_formats = formats.Select (f => new { Format = f, Order = Math.Max (format_order.IndexOf (", " + f.ToLower () + ","), format_order.IndexOf (f.ToLower ())) })
                                        .OrderBy (o => o.Order == -1 ? Int32.MaxValue : o.Order);

            // See if all the files contain their track #
            bool all_tracks_have_num_in_title = tracks.All (t => t.TrackNumber == 0 || t.TrackTitle.Contains (t.TrackNumber.ToString ()));

            // Make these columns snugly fix their data
            if (tracks.Count > 0) {
                // Mono in openSUSE 11.0 doesn't like this
                //SetWidth (columns.TrackColumn,    all_tracks_have_num_in_title ? 0 : tracks.Max (f => f.TrackNumber), 0);
                int max_track = 0;
                long max_size = 0;
                foreach (var t in tracks) {
                    max_track = Math.Max (max_track, t.TrackNumber);
                    max_size = Math.Max (max_size, t.FileSize);
                }
                SetWidth (columns.TrackColumn,    all_tracks_have_num_in_title ? 0 : max_track, 0);

                // Mono in openSUSE 11.0 doesn't like this
                //SetWidth (columns.FileSizeColumn, tracks.Max (f => f.FileSize), 0);
                SetWidth (columns.FileSizeColumn, max_size, 0);
                SetWidth (columns.DurationColumn, tracks.Max (f => f.Duration), TimeSpan.Zero);
            }

            string max_title = "     ";
            if (tracks.Count > 0) {
                var sorted_by_title = files.Where (t => !t.Location.Contains ("zip"))
                                           .OrderBy (f => f.Title == null ? 0 : f.Title.Length)
                                           .ToList ();
                string nine_tenths = sorted_by_title[(int)Math.Floor (.90 * sorted_by_title.Count)].Title ?? "";
                string max = sorted_by_title[sorted_by_title.Count - 1].Title ?? "";
                max_title = ((double)max.Length >= (double)(1.6 * (double)nine_tenths.Length)) ? nine_tenths : max;
            }
            (columns.TitleColumn.GetCell (0) as ColumnCellText).SetMinMaxStrings (max_title);

            file_list.ColumnController = file_columns;
            file_list.SetModel (files_model);

            var format_list = ComboBox.NewText ();
            format_list.RowSeparatorFunc = (model, iter) => {
                return (string)model.GetValue (iter, 0) == "---";
            };

            bool have_sep = false;
            int active_format = 0;
            foreach (var fmt in sorted_formats) {
                if (fmt.Order == -1 && !have_sep) {
                    have_sep = true;
                    if (format_list.Model.IterNChildren () > 0) {
                        format_list.AppendText ("---");
                    }
                }

                format_list.AppendText (fmt.Format);

                if (active_format == 0 && fmt.Format == item.SelectedFormat) {
                    active_format = format_list.Model.IterNChildren () - 1;
                }
            }

            format_list.Changed += (o, a) => {
                files_model.Clear ();

                var selected_fmt = format_list.ActiveText;
                foreach (var track in tracks) {
                    if (track.MimeType == selected_fmt) {
                        files_model.Add (track);
                    }
                }

                files_model.Reload ();

                item.SelectedFormat = selected_fmt;
                item.Save ();
            };

            if (formats.Count > 0) {
                format_list.Active = active_format;
            }

            vbox.PackStart (file_sw, true, true, 0);
            vbox.PackStart (format_list, false, false, 0);

            file_list.SizeAllocated += (o, a) => {
                int target_list_width = file_list.MaxWidth;
                if (file_sw.VScrollbar != null && file_sw.VScrollbar.IsMapped) {
                    target_list_width += file_sw.VScrollbar.Allocation.Width + 2;
                }

                // Don't let the track list be too wide
                target_list_width = Math.Min (target_list_width, (int) (0.5 * (double)Allocation.Width));

                if (a.Allocation.Width != target_list_width && target_list_width > 0) {
                    file_sw.SetSizeRequest (target_list_width, -1);
                }
            };

            PackStart (vbox, false, false, 0);
        }*/

        /*private void SetWidth<T> (Column col, T max, T zero)
        {
            (col.GetCell (0) as ColumnCellText).SetMinMaxStrings (max, max);
            if (zero.Equals (max)) {
                col.Visible = false;
            }
        }*/
    }
}
