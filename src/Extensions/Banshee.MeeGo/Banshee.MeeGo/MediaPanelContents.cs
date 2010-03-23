//
// MediaPanelContents.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Novell, Inc.
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

// using Banshee.PlayQueue;

using Hyena;
using Hyena.Gui;
using Hyena.Data.Gui;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.Collection.Database;

namespace Banshee.MeeGo
{
    public class MediaPanelContents : Table
    {
        private SourceComboBox source_combo_box;
        private HBox header_box;
        private SearchEntry search_entry;
        private AlbumListView album_view;
        private TerseTrackListView track_view;

        public MediaPanelContents () : base (1, 2, false)
        {
            BorderWidth = 12;
            RowSpacing = 15;
            ColumnSpacing = 15;
        }

        public void BuildViews ()
        {
            header_box = new HBox () {
                Spacing = 5,
                BorderWidth = 5
            };
            header_box.PackStart (source_combo_box = new SourceComboBox (), false, false, 0);
            var button = new Button (new Image () {
                IconSize = (int)IconSize.LargeToolbar,
                IconName = "media-player-banshee"
            }) {
                TooltipText = Catalog.GetString ("Launch the Banshee Media Player")
            };
            button.Clicked += (o, e) => {
                ServiceManager.SourceManager.SetActiveSource (ServiceManager.SourceManager.MusicLibrary);
                ServiceManager.Get<MeeGoService> ().PresentPrimaryInterface ();
            };
            header_box.PackStart (button, false, false, 0);
            header_box.PackStart (search_entry = new SearchEntry () { Ready = true }, true, true, 0);
            Attach (header_box, 0, 1, 0, 1,
                AttachOptions.Fill | AttachOptions.Expand,
                AttachOptions.Shrink,
                0, 0);

            var scroll = new ScrolledWindow () {
                VscrollbarPolicy = PolicyType.Always,
                HscrollbarPolicy = PolicyType.Never,
                ShadowType = ShadowType.None
            };
            scroll.Add (album_view = new AlbumListView ());
            Attach (scroll, 0, 1, 1, 2,
                AttachOptions.Fill | AttachOptions.Expand,
                AttachOptions.Fill | AttachOptions.Expand,
                0, 0);

            var side_box = new VBox () { Spacing = 15 };

            side_box.PackStart (new PlaybackBox (), false, false, 0);

            scroll = new ScrolledWindow () {
                VscrollbarPolicy = PolicyType.Always,
                HscrollbarPolicy = PolicyType.Never,
                ShadowType = ShadowType.None
            };
            track_view = new TerseTrackListView () {
                IsReorderable = true
            };
            track_view.ColumnController.Insert (new Column (null, "indicator",
                new ColumnCellStatusIndicator (null), 0.05, true, 20, 20), 0);
            track_view.ColumnController.Add (new Column ("Rating", new ColumnCellRating ("Rating", false), 0.15));
            scroll.Add (track_view);

            side_box.PackStart (scroll, true, true, 0);

            side_box.PackStart (new MeeGoTrackInfoDisplay () { HeightRequest = 64 }, false, false, 0);
            Attach (side_box, 1, 2, 0, 2,
                AttachOptions.Shrink,
                AttachOptions.Fill | AttachOptions.Expand,
                0, 0);

            ShowAll ();

            source_combo_box.UpdateActiveSource ();
            source_combo_box.Model.Filter = (source) => source is ITrackModelSource;
            search_entry.Changed += OnSearchEntryChanged;
            ServiceManager.SourceManager.ActiveSourceChanged += OnActiveSourceChanged;
        }

        private void OnSearchEntryChanged (object o, EventArgs args)
        {
            var source = ServiceManager.SourceManager.ActiveSource;
            if (source == null) {
                return;
            }

            source.FilterType = (TrackFilterType)search_entry.ActiveFilterID;
            source.FilterQuery = search_entry.Query;
        }

        private void OnActiveSourceChanged (SourceEventArgs args)
        {
            ThreadAssist.ProxyToMain (delegate {
                var source = ServiceManager.SourceManager.ActiveSource;
                var track_source = source as ITrackModelSource;
                var filter_source = source as IFilterableSource;

                if (track_source == null) {
                    return;
                }

                track_view.SetModel (track_source.TrackModel);

                if (filter_source == null) {
                    album_view.Parent.Hide ();
                    return;
                }

                foreach (var filter in filter_source.CurrentFilters) {
                    var album_filter = filter as DatabaseAlbumListModel;
                    if (album_filter != null) {
                        album_view.Parent.Show ();
                        album_view.SetModel (album_filter);
                        return;
                    }
                }

                album_view.Parent.Hide ();
            });
        }

        protected override void OnParentSet (Widget previous)
        {
            base.OnParentSet (previous);

            if (Parent != null) {
                Parent.ModifyBg (StateType.Normal, Style.White);
            }
        }

        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            if (!Visible || !IsMapped) {
                return true;
            }

            RenderBackground (evnt.Window, evnt.Region);
            foreach (var child in Children) {
                PropagateExpose (child, evnt);
            }

            return true;
        }

        private void RenderBackground (Gdk.Window window, Gdk.Region region)
        {
            var cr = Gdk.CairoHelper.Create (window);
            cr.Color = new Cairo.Color (0xe7 / (double)0xff,
                0xea / (double)0xff, 0xfd / (double)0xff);

            CairoExtensions.RoundedRectangle (cr,
                header_box.Allocation.X,
                header_box.Allocation.Y,
                header_box.Allocation.Width,
                header_box.Allocation.Height,
                5);

            cr.Fill ();

            CairoExtensions.DisposeContext (cr);
        }
    }
}
