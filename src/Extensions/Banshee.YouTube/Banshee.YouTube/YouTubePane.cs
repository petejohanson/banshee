//
// YouTubePane.cs
//
// Authors:
//   Kevin Duffus <KevinDuffus@gmail.com>
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//
// Copyright (C) 2009 Kevin Duffus
// Copyright (C) 2010 Alexander Kojevnikov
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
using System.Threading;

using Mono.Unix;
using Gtk;

using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.YouTube;
using Google.YouTube;

using Hyena;
using Hyena.Jobs;
using Hyena.Widgets;

using Banshee.Gui;
using Banshee.Gui.Widgets;
using Banshee.Widgets;
using Banshee.Collection;

using Banshee.YouTube.Data;
using Banshee.YouTube.Gui;

namespace Banshee.YouTube
{
    public class YouTubePane : Gtk.ScrolledWindow
    {
        private ContextPage yt_context_page;
        private TileView results_tv;
        private Label no_results_label = new Label (Catalog.GetString ("No videos found"));
        private Scheduler scheduler = new Scheduler ();
        private Job refresh_videos_jobs;

        private int max_results_display = 12;
        private bool showing_results = true;
        private bool ready = false;
        private bool refreshing = false;
        private bool show_when_ready = true;
        private bool ShowWhenReady {
            get { return show_when_ready; }
            set {
                show_when_ready = value;
                ShowIfReady ();

                if (!show_when_ready) {
                    CancelTasks ();
                } else if (!ready && !refreshing) {
                    RefreshVideos ();
                }
            }
        }

        private void ShowIfReady ()
        {
            if (ShowWhenReady && ready) {
                ShowAll ();
            }
        }

        private string query;
        public string Query {
            get { return query; }
            set {
                if (query == value) {
                    return;
                }

                ready = false;
                query = value;

                if (!String.IsNullOrEmpty (query)) {
                    RefreshVideos ();
                }
            }
        }

        private void RefreshVideos ()
        {
            CancelTasks ();

            if (show_when_ready && !String.IsNullOrEmpty (Query)) {
                refreshing = true;
                yt_context_page.SetState (Banshee.ContextPane.ContextState.Loading);
                refresh_videos_jobs = new RefreshVideosJob (this, Query);
                scheduler.Add (refresh_videos_jobs);
            }
        }

        private void CancelTasks ()
        {
            scheduler.Cancel (refresh_videos_jobs);
            refreshing = false;
        }

        public void HideWithTimeout ()
        {
            GLib.Timeout.Add (200, OnHideTimeout);
        }

        public bool OnHideTimeout ()
        {
            if (!ShowWhenReady || !ready) {
                Hide ();
            }
            return false;
        }

        public YouTubePane (ContextPage context_page) : base ()
        {
            this.yt_context_page = context_page;

            results_tv = new TileView (max_results_display);

            Add (results_tv);
            ShowAll ();
        }

        protected override void OnStyleSet (Style previous_style)
        {
            base.OnStyleSet (previous_style);
            results_tv.ModifyBg (StateType.Normal, Style.Base (StateType.Normal));
        }

        private class RefreshVideosJob : SimpleAsyncJob
        {
            private YouTubePane yt_pane;
            private string yt_query_val;

            public RefreshVideosJob (YouTubePane pane, string query_val)
            {
                this.yt_pane = pane;
                this.yt_query_val = query_val;
            }

            protected override void Run ()
            {
                DataCore yt_data = new DataCore();
                bool init_request = yt_data.InitYouTubeRequest();

                if (yt_data == null || !init_request) {
                    return;
                }

                yt_data.PerformSearch (yt_query_val);
                yt_pane.UpdateForQuery (yt_data.Videos);
            }
        }

        private void UpdateForQuery (Feed<Video> video_feed)
        {
            int result_display_count = 0;
            var tiles = new List<YouTubeTileData> ();
            bool cleanup;

            if (video_feed.TotalResults > 0) {
                cleanup = !showing_results;

                foreach (Video entry in video_feed.Entries) {
                    // Don't include videos that are not live
                    if (entry.IsDraft) {
                        continue;
                    } else if (result_display_count++ < max_results_display) {
                        tiles.Add (new YouTubeTileData (entry));
                    }
                }

                showing_results = true;
            } else {
                Log.Debug ("YouTube: No videos found");
                cleanup = showing_results;
                showing_results = false;
            }

            ThreadAssist.BlockingProxyToMain (delegate {
                results_tv.ClearWidgets ();

                if (showing_results) {
                    if (cleanup) {
                        Remove (no_results_label);
                        Add (results_tv);
                        ShowAll ();
                    }

                    foreach (YouTubeTileData tile in tiles) {
                        results_tv.AddWidget (new YouTubeTile (tile));
                    }
                    results_tv.ShowAll ();
                } else if (cleanup) {
                    Remove (results_tv);
                    Add (no_results_label);
                    ShowAll ();
                }

                ready = true;
                refreshing = false;
                yt_context_page.SetState (Banshee.ContextPane.ContextState.Loaded);
            });
        }
    }
}
