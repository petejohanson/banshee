//
// PlayQueueActions.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

using Mono.Unix;
using Gtk;

using Hyena;

using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Gui;

namespace Banshee.PlayQueue
{
    public class PlayQueueActions : Banshee.Gui.BansheeActionGroup
    {
        private PlayQueueSource playqueue;
        public PlayQueueActions (PlayQueueSource playqueue) : base ("playqueue")
        {
            this.playqueue = playqueue;

            Add (new ActionEntry [] {
                new ActionEntry ("AddToPlayQueueAction", Stock.Add,
                    Catalog.GetString ("Add to Play Queue"), "q",
                    Catalog.GetString ("Append selected songs to the play queue"),
                    OnAddToPlayQueue),

                new ActionEntry ("AddToPlayQueueAfterAction", null,
                    Catalog.GetString ("Play After"), null,
                    Catalog.GetString ("Add selected songs after the currently playing track, album, or artist"),
                    null),

                new ActionEntry ("AddToPlayQueueAfterCurrentTrackAction", null,
                    Catalog.GetString ("Current Track"), null,
                    Catalog.GetString ("Add selected songs to the play queue after the currently playing song"),
                    OnAddToPlayQueueAfterCurrentTrack),

                new ActionEntry ("AddToPlayQueueAfterCurrentAlbumAction", null,
                    Catalog.GetString ("Current Album"), null,
                    Catalog.GetString ("Add selected songs to the play queue after the currently playing album"),
                    OnAddToPlayQueueAfterCurrentAlbum),

                new ActionEntry ("AddToPlayQueueAfterCurrentArtistAction", null,
                    Catalog.GetString ("Current Artist"), null,
                    Catalog.GetString ("Add selected songs to the play queue after the currently playing artist"),
                    OnAddToPlayQueueAfterCurrentArtist)
            });

            AddImportant (
                new ActionEntry ("RefreshPlayQueueAction", Stock.Refresh,
                    Catalog.GetString ("Refresh"), null,
                    Catalog.GetString ("Refresh random tracks in the play queue"),
                    OnRefreshPlayQueue),

                new ActionEntry ("ShufflePlayQueue", null,
                    Catalog.GetString ("Shuffle"), null,
                    Catalog.GetString ("Randomize the playback order of items in the play queue"),
                    OnShufflePlayQueue),

                new ActionEntry ("AddPlayQueueTracksAction", Stock.Add,
                    Catalog.GetString ("Add More"), null,
                    Catalog.GetString ("Add more random tracks to the play queue"),
                    OnAddPlayQueueTracks),

                new ActionEntry ("ClearPlayQueueAction", Stock.Clear,
                    Catalog.GetString ("Clear"), null,
                    Catalog.GetString ("Remove all tracks from the play queue"),
                    OnClearPlayQueue)
            );

            this["ShufflePlayQueue"].IconName = "media-playlist-shuffle";

            Add (new ToggleActionEntry [] {
                new ToggleActionEntry ("ClearPlayQueueOnQuitAction", null,
                    Catalog.GetString ("Clear on Quit"), null,
                    Catalog.GetString ("Clear the play queue when quitting"),
                    OnClearPlayQueueOnQuit, PlayQueueSource.ClearOnQuitSchema.Get ())
            });

            AddUiFromFile ("GlobalUI.xml");

            playqueue.Updated += OnUpdated;
            ServiceManager.SourceManager.ActiveSourceChanged += OnSourceUpdated;
            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent, PlayerEvent.StateChange);
            ServiceManager.PlaybackController.SourceChanged += OnPlaybackSourceChanged;

            OnUpdated (null, null);

            Register ();
        }

        public override void Dispose ()
        {
            playqueue.Updated -= OnUpdated;
            ServiceManager.SourceManager.ActiveSourceChanged -= OnSourceUpdated;
            base.Dispose ();
        }

        #region Action Handlers

        private void OnPlayerEvent (PlayerEventArgs args)
        {
            this["AddToPlayQueueAfterAction"].Sensitive = ServiceManager.PlayerEngine.IsPlaying ();
        }

        private void OnPlaybackSourceChanged (object sender, EventArgs e)
        {
            if (ServiceManager.PlaybackController.Source is PlayQueueSource) {
                this["AddToPlayQueueAfterCurrentAlbumAction"].Sensitive = true;
                this["AddToPlayQueueAfterCurrentArtistAction"].Sensitive = true;
            } else {
                this["AddToPlayQueueAfterCurrentAlbumAction"].Sensitive = false;
                this["AddToPlayQueueAfterCurrentArtistAction"].Sensitive = false;
            }
        }

        private void OnAddToPlayQueue (object o, EventArgs args)
        {
            AddSelectedToPlayQueue (QueueMode.Normal);
        }

        private void OnAddToPlayQueueAfterCurrentTrack (object sender, EventArgs e)
        {
            AddSelectedToPlayQueue (QueueMode.AfterCurrentTrack);
        }

        private void OnAddToPlayQueueAfterCurrentAlbum (object sender, EventArgs e)
        {
            AddSelectedToPlayQueue (QueueMode.AfterCurrentAlbum);
        }

        private void OnAddToPlayQueueAfterCurrentArtist (object sender, EventArgs e)
        {
            AddSelectedToPlayQueue (QueueMode.AfterCurrentArtist);
        }

        private void AddSelectedToPlayQueue (QueueMode mode)
        {
            var track_actions = ServiceManager.Get<InterfaceActionService> ().TrackActions;
            playqueue.AddSelectedTracks (ServiceManager.SourceManager.ActiveSource, track_actions.Selection, mode);
        }

        private void OnClearPlayQueue (object o, EventArgs args)
        {
            playqueue.Clear ();
        }

        private void OnRefreshPlayQueue (object o, EventArgs args)
        {
            playqueue.Refresh ();
        }

        private void OnAddPlayQueueTracks (object o, EventArgs args)
        {
            playqueue.AddMoreRandomTracks ();
        }

        private void OnShufflePlayQueue (object o, EventArgs args)
        {
            playqueue.Shuffle ();
        }

        private void OnClearPlayQueueOnQuit (object o, EventArgs args)
        {
            ToggleAction action = this["ClearPlayQueueOnQuitAction"] as Gtk.ToggleAction;
            PlayQueueSource.ClearOnQuitSchema.Set (action.Active);
        }

        #endregion

        private void OnSourceUpdated (SourceEventArgs args)
        {
            if (ServiceManager.SourceManager.ActiveSource is PlayQueueSource) {
                this["AddToPlayQueueAfterAction"].Visible = false;
            } else {
                this["AddToPlayQueueAfterAction"].Visible = true;
            }
            
            OnUpdated (null, null);
        }

        private void OnUpdated (object o, EventArgs args)
        {
            ThreadAssist.ProxyToMain (UpdateActions);
        }

        private void UpdateActions ()
        {
            Source source = ServiceManager.SourceManager.ActiveSource;
            if (source != null) {
                DatabaseSource db_source = source as DatabaseSource ?? source.Parent as DatabaseSource;
                UpdateAction ("RefreshPlayQueueAction", playqueue.Populate);
                UpdateAction ("AddPlayQueueTracksAction", playqueue.Populate);
                UpdateAction ("ShufflePlayQueue", !playqueue.Populate, playqueue.Count > 1);
                UpdateAction ("ClearPlayQueueAction", true, playqueue.Count > 0);
                UpdateAction ("AddToPlayQueueAction", db_source != null && db_source != playqueue, true);
            }
        }
    }
}
