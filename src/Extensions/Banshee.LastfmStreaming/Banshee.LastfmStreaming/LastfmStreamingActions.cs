//
// LastfmStreamingActions.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
//
// Copyright 2007-2008 Novell, Inc.
// Copyright 2010 Bertrand Lorentz
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
using Gtk;

using Mono.Unix;

using Banshee.Collection;
using Banshee.Gui;
using Banshee.Lastfm;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Sources;

using Lastfm;

namespace Banshee.LastfmStreaming.Radio
{
    public class LastfmStreamingActions : BansheeActionGroup
    {
        private LastfmSource lastfm;
        private uint actions_id;
        
        public LastfmStreamingActions (LastfmSource lastfm) : base (ServiceManager.Get<InterfaceActionService> (), "LastfmStreaming")
        {
            this.lastfm = lastfm;

            AddImportant (
                new ActionEntry (
                    "LastfmAddAction", Stock.Add,
                     Catalog.GetString ("_Add Station..."),
                     null, Catalog.GetString ("Add a new Last.fm radio station"), OnAddStation
                )
            );

            Add (new ActionEntry [] {
                new ActionEntry (
                    "RefreshSourceAction", Stock.Refresh,
                     Catalog.GetString ("Refresh"), null,
                     String.Empty, OnRefreshSource
                )
            });

            // Translators: {0} is a type of Last.fm station, eg "Fans of" or "Similar to".
            string listen_to = Catalog.GetString ("Listen to {0} Station");
            // Translators: {0} is a type of Last.fm station, eg "Fans of" or "Similar to".
            string listen_to_long = Catalog.GetString ("Listen to the Last.fm {0} station for this artist");

            // Artist actions
            Add (new ActionEntry [] {
                new ActionEntry ("LastfmArtistPlayFanRadioAction", StationType.Fan.IconName,
                    String.Format (listen_to, String.Format ("'{0}'", Catalog.GetString ("Fans of"))), null,
                    String.Format (listen_to_long, String.Format ("'{0}'", Catalog.GetString ("Fans of"))),
                    OnArtistPlayFanRadio),

                new ActionEntry ("LastfmArtistPlaySimilarRadioAction", StationType.Similar.IconName,
                    String.Format (listen_to, String.Format ("'{0}'", Catalog.GetString ("Similar to"))), null,
                    String.Format (listen_to_long, String.Format ("'{0}'", Catalog.GetString ("Similar to"))),
                    OnArtistPlaySimilarRadio)
            });

            // Track actions
            Add (new ActionEntry [] {
                new ActionEntry (
                    "LastfmLoveAction", null,
                    Catalog.GetString ("Love Track"), null,
                    Catalog.GetString ("Mark current track as loved"), OnLoved),

                new ActionEntry (
                    "LastfmHateAction", null,
                    Catalog.GetString ("Ban Track"), null,
                    Catalog.GetString ("Mark current track as banned"), OnHated)
            });

            this["LastfmLoveAction"].IconName = "face-smile";
            this["LastfmHateAction"].IconName = "face-sad";

            this["LastfmLoveAction"].IsImportant = true;
            this["LastfmHateAction"].IsImportant = true;

            actions_id = Actions.UIManager.AddUiFromResource ("GlobalUI.xml");
            Actions.AddActionGroup (this);

            lastfm.Connection.StateChanged += HandleConnectionStateChanged;
            Actions.SourceActions ["SourcePropertiesAction"].Activated += OnSourceProperties;
            ServiceManager.PlaybackController.SourceChanged += OnPlaybackSourceChanged;
            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent,
                PlayerEvent.StartOfStream |
                PlayerEvent.EndOfStream);
            UpdateActions ();
        }

        public override void Dispose ()
        {
            Actions.UIManager.RemoveUi (actions_id);
            Actions.RemoveActionGroup (this);
            lastfm.Connection.StateChanged -= HandleConnectionStateChanged;
            Actions.SourceActions ["SourcePropertiesAction"].Activated -= OnSourceProperties;
            ServiceManager.PlaybackController.SourceChanged -= OnPlaybackSourceChanged;
            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);
            base.Dispose ();
        }

#region Action Handlers

        private void OnAddStation (object sender, EventArgs args)
        {
            StationEditor ed = new StationEditor (lastfm);
            ed.RunDialog ();
        }

        private void OnSourceProperties (object o, EventArgs args)
        {
            Source source = Actions.SourceActions.ActionSource;
            if (source is StationSource) {
                StationEditor editor = new StationEditor (lastfm, source as StationSource);
                editor.RunDialog ();
            }
        }

        private void OnRefreshSource (object o, EventArgs args)
        {
            StationSource source = Actions.SourceActions.ActionSource as StationSource;
            if (source != null) {
                source.Refresh();
            }
        }

        private void OnLoved (object sender, EventArgs args)
        {
            LastfmTrackInfo track = ServiceManager.PlayerEngine.CurrentTrack as LastfmTrackInfo;
            if (track == null)
                return;

            track.Love ();
        }

        private void OnHated (object sender, EventArgs args)
        {
            LastfmTrackInfo track = ServiceManager.PlayerEngine.CurrentTrack as LastfmTrackInfo;
            if (track == null)
                return;

            track.Ban ();
            ServiceManager.PlaybackController.Next ();
        }

        private void OnArtistPlayFanRadio (object sender, EventArgs args)
        {
            StationSource fan_radio = null;
            foreach (StationSource station in lastfm.Children) {
                if (station.Type == StationType.Fan && station.Arg == lastfm.Actions.CurrentArtist) {
                    fan_radio = station;
                    break;
                }
            }

            if (fan_radio == null) {
                fan_radio = new StationSource (lastfm,
                    String.Format (Catalog.GetString ("Fans of {0}"), lastfm.Actions.CurrentArtist),
                    "Fan", lastfm.Actions.CurrentArtist
                );
                lastfm.AddChildSource (fan_radio);
            }

            ServiceManager.SourceManager.SetActiveSource (fan_radio);
        }

        private void OnArtistPlaySimilarRadio (object sender, EventArgs args)
        {
            StationSource similar_radio = null;
            foreach (StationSource station in lastfm.Children) {
                if (station.Type == StationType.Similar && station.Arg == lastfm.Actions.CurrentArtist) {
                    similar_radio = station;
                    break;
                }
            }

            if (similar_radio == null) {
                similar_radio = new StationSource (lastfm,
                    String.Format (Catalog.GetString ("Similar to {0}"), lastfm.Actions.CurrentArtist),
                    "Similar", lastfm.Actions.CurrentArtist
                );
                lastfm.AddChildSource (similar_radio);
            }

            ServiceManager.SourceManager.SetActiveSource (similar_radio);
        }

#endregion
        private void OnPlayerEvent (PlayerEventArgs args)
        {
            UpdateActions ();
        }

        private void HandleConnectionStateChanged (object sender, ConnectionStateChangedArgs args)
        {
            UpdateActions ();
        }

        private bool updating = false;
        private void UpdateActions ()
        {
            lock (this) {
                if (updating)
                    return;
                updating = true;
            }

            bool have_user = (lastfm.Account != null && lastfm.Account.UserName != null);
            this["LastfmAddAction"].Sensitive = have_user;
            this["LastfmAddAction"].ShortLabel = Catalog.GetString ("_Add Station");

            TrackInfo current_track = ServiceManager.PlayerEngine.CurrentTrack;
            this["LastfmLoveAction"].Visible = current_track is LastfmTrackInfo;
            this["LastfmHateAction"].Visible = current_track is LastfmTrackInfo;

            updating = false;
        }

        private uint track_actions_id;
        private bool was_lastfm = false;
        private void OnPlaybackSourceChanged (object o, EventArgs args)
        {
            if (Actions == null || Actions.PlaybackActions == null || ServiceManager.PlaybackController == null)
                return;

            UpdateActions ();

            bool is_lastfm = ServiceManager.PlaybackController.Source is StationSource;
            Actions.PlaybackActions["PreviousAction"].Sensitive = !is_lastfm;

            if (is_lastfm && !was_lastfm)
                track_actions_id = Actions.UIManager.AddUiFromResource ("LastfmTrackActions.xml");
            else if (!is_lastfm && was_lastfm)
                Actions.UIManager.RemoveUi (track_actions_id);

            was_lastfm = is_lastfm;
        }
    }
}
