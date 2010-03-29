// 
// MeeGoSourceContents.cs
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
using System.Collections.Generic;

using Gtk;

using Hyena.Data;
using Hyena.Data.Gui;

using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.Collection;
using Banshee.Collection.Gui;

namespace Banshee.MeeGo
{
    public class MeeGoSourceContents : HBox, ITrackModelSourceContents
    {
        private ArtistListView artist_view;
        private AlbumListView album_view;
        private TerseTrackListView track_view;

        private ISource source;
        private Dictionary<object, double> model_positions = new Dictionary<object, double> ();

        public MeeGoSourceContents ()
        {
            Spacing = 5;

            var side_box = new VBox () {
                Spacing = 5
            };

            PackStart (SetupView (artist_view = new ArtistListView ()), false, false, 0);
            PackStart (SetupView (album_view = new AlbumListView ()), true, true, 0);
            PackStart (side_box, false, false, 0);
            side_box.PackStart (SetupView (track_view = new TerseTrackListView ()), true, true, 0);
            track_view.ColumnController.Insert (new Column (null, "indicator",
                new ColumnCellStatusIndicator (null), 0.05, true, 20, 20), 0);
            side_box.PackStart (new MeeGoTrackInfoDisplay () { HeightRequest = 64 }, false, false, 0);

            artist_view.WidthRequest = 150;
            track_view.WidthRequest = 240;
            artist_view.DoNotRenderNullModel = true;
            album_view.DoNotRenderNullModel = true;

            artist_view.SelectionProxy.Changed += OnBrowserViewSelectionChanged;
            album_view.SelectionProxy.Changed += OnBrowserViewSelectionChanged;
        }

        private ScrolledWindow SetupView (Widget view)
        {
            var scrolled = new ScrolledWindow () {
                VscrollbarPolicy = PolicyType.Automatic,
                HscrollbarPolicy = PolicyType.Never,
                ShadowType = ShadowType.None
            };
            scrolled.Add (view);
            return scrolled;
        }

        private void OnBrowserViewSelectionChanged (object o, EventArgs args)
        {
            // Scroll the raising filter view to the top if "all" is selected
            var selection = (Hyena.Collections.Selection)o;
            if (!selection.AllSelected) {
                return;
            }

            if (artist_view.Selection == selection) {
                artist_view.ScrollTo (0);
            } else if (album_view.Selection == selection) {
                album_view.ScrollTo (0);
            }
        }

#region View<->Model binding

        private void SetModel<T> (IListModel<T> model)
        {
            ListView<T> view = FindListView <T> ();
            if (view != null) {
                SetModel (view, model);
            } else {
                Hyena.Log.DebugFormat ("Unable to find view for model {0}", model);
            }
        }

        private void SetModel<T> (ListView<T> view, IListModel<T> model)
        {
            if (view.Model != null) {
                model_positions[view.Model] = view.Vadjustment.Value;
            }

            if (model == null) {
                view.SetModel (null);
                return;
            }

            if (!model_positions.ContainsKey (model)) {
                model_positions[model] = 0.0;
            }

            view.SetModel (model, model_positions[model]);
        }

        private ListView<T> FindListView<T> ()
        {
            foreach (var view in new IListView [] { artist_view, album_view, track_view }) {
                if (view is ListView<T>) {
                    return (ListView<T>)view;
                }
            }
            return null;
        }

#endregion

#region ISourceContents

        public bool SetSource (ISource source)
        {
            var track_source = source as ITrackModelSource;
            var filterable_source = source as IFilterableSource;
            if (track_source == null) {
                return false;
            }

            this.source = source;

            SetModel (track_view, track_source.TrackModel);

            if (filterable_source != null && filterable_source.CurrentFilters != null) {
                foreach (var model in filterable_source.CurrentFilters) {
                    if (model is IListModel<ArtistInfo>) {
                        SetModel (artist_view, (model as IListModel<ArtistInfo>));
                    } else if (model is IListModel<AlbumInfo>) {
                        SetModel (album_view, (model as IListModel<AlbumInfo>));
                    }
                }
            }

            return true;
        }

        public void ResetSource ()
        {
            source = null;
            SetModel (track_view, null);
            SetModel (artist_view, null);
            SetModel (album_view, null);
            track_view.HeaderVisible = false;
        }

        public ISource Source {
            get { return source; }
        }

        public Widget Widget {
            get { return this; }
        }

#endregion

#region ITrackModelSourceContents

        public IListView<TrackInfo> TrackView {
            get { return track_view; }
        }

#endregion

    }
}

