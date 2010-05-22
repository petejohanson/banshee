//
// LastfmActions.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using Gtk;

using Mono.Unix;

using Lastfm;
using Lastfm.Gui;
using SortType = Hyena.Data.SortType;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Widgets;
using Banshee.MediaEngine;
using Banshee.Database;
using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.Gui;
using Banshee.Collection;
using Banshee.PlaybackController;

using Browser = Banshee.Web.Browser;

namespace Banshee.Lastfm
{
    public class LastfmActions : BansheeActionGroup
    {
        private LastfmSource lastfm;
        private uint actions_id;

        public LastfmActions (LastfmSource lastfm) : base (ServiceManager.Get<InterfaceActionService> (), "Lastfm")
        {
            this.lastfm = lastfm;

            Add (new ActionEntry [] {
                new ActionEntry (
                    "LastfmConnectAction", null,
                     Catalog.GetString ("Connect"),
                     null, String.Empty, OnConnect
                )
            });

            // Artist actions
            Add (new ActionEntry [] {
                new ActionEntry ("LastfmArtistVisitLastfmAction", "audioscrobbler",
                    Catalog.GetString ("View on Last.fm"), null,
                    Catalog.GetString ("View this artist's Last.fm page"), OnArtistVisitLastfm),

                new ActionEntry ("LastfmArtistVisitWikipediaAction", "",
                    Catalog.GetString ("View Artist on Wikipedia"), null,
                    Catalog.GetString ("Find this artist on Wikipedia"), OnArtistVisitWikipedia),

                /*new ActionEntry ("LastfmArtistVisitAmazonAction", "",
                    Catalog.GetString ("View Artist on Amazon"), null,
                    Catalog.GetString ("Find this artist on Amazon"), OnArtistVisitAmazon),*/

                new ActionEntry ("LastfmArtistViewVideosAction", "",
                    Catalog.GetString ("View Artist's Videos"), null,
                    Catalog.GetString ("Find videos by this artist"), OnArtistViewVideos),

                new ActionEntry ("LastfmArtistRecommendAction", "",
                    Catalog.GetString ("Recommend to"), null,
                    Catalog.GetString ("Recommend this artist to someone"), OnArtistRecommend)

            });

            // Album actions
            Add (new ActionEntry [] {
                new ActionEntry ("LastfmAlbumVisitLastfmAction", "audioscrobbler.png",
                    Catalog.GetString ("View on Last.fm"), null,
                    Catalog.GetString ("View this album's Last.fm page"), OnAlbumVisitLastfm),

                /*new ActionEntry ("LastfmAlbumVisitAmazonAction", "",
                    Catalog.GetString ("View Album on Amazon"), null,
                    Catalog.GetString ("Find this album on Amazon"), OnAlbumVisitAmazon),*/

                new ActionEntry ("LastfmAlbumRecommendAction", "",
                    Catalog.GetString ("Recommend to"), null,
                    Catalog.GetString ("Recommend this album to someone"), OnAlbumRecommend)
            });

            // Track actions
            Add (new ActionEntry [] {
                new ActionEntry ("LastfmTrackVisitLastfmAction", "audioscrobbler",
                    Catalog.GetString ("View on Last.fm"), null,
                    Catalog.GetString ("View this track's Last.fm page"), OnTrackVisitLastfm),

                new ActionEntry ("LastfmTrackRecommendAction", "",
                    Catalog.GetString ("Recommend to"), null,
                    Catalog.GetString ("Recommend this track to someone"), OnTrackRecommend)
            });

            actions_id = Actions.UIManager.AddUiFromResource ("GlobalUI.xml");
            Actions.AddActionGroup (this);

            lastfm.Connection.StateChanged += HandleConnectionStateChanged;
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
            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);
            base.Dispose ();
        }

#region Action Handlers

        private void OnConnect (object sender, EventArgs args)
        {
            lastfm.Connection.Connect ();
        }

        private void OnArtistVisitLastfm (object sender, EventArgs args)
        {
            Browser.Open (String.Format (
                Catalog.GetString ("http://last.fm/music/{0}"),
                Encode (CurrentArtist)
            ));
        }

        private void OnAlbumVisitLastfm (object sender, EventArgs args)
        {
            Browser.Open (String.Format (
                Catalog.GetString ("http://last.fm/music/{0}/{1}"),
                Encode (CurrentArtist), Encode (CurrentAlbum)
            ));
        }

        private void OnTrackVisitLastfm (object sender, EventArgs args)
        {
            Browser.Open (String.Format (
                Catalog.GetString ("http://last.fm/music/{0}/_/{1}"),
                Encode (CurrentArtist), Encode (CurrentTrack)
            ));
        }

        private void OnArtistViewVideos (object sender, EventArgs args)
        {
            Browser.Open (String.Format (
                Catalog.GetString ("http://www.last.fm/music/{0}/+videos"),
                Encode (CurrentArtist)
            ));
        }

        private void OnArtistVisitWikipedia (object sender, EventArgs args)
        {
            Browser.Open (String.Format (
                Catalog.GetString ("http://en.wikipedia.org/wiki/{0}"),
                Encode ((CurrentArtist ?? String.Empty).Replace (' ', '_'))
            ));
        }

        private static string Encode (string i)
        {
            return System.Web.HttpUtility.UrlEncode (i);
        }

        /*private void OnArtistVisitAmazon (object sender, EventArgs args)
        {
            Browser.Open (String.Format (
                Catalog.GetString ("http://amazon.com/wiki/{0}"),
                CurrentArtist
            ));
        }

        private void OnAlbumVisitAmazon (object sender, EventArgs args)
        {
        }*/

        private void OnArtistRecommend (object sender, EventArgs args)
        {
        }

        private void OnAlbumRecommend (object sender, EventArgs args)
        {
        }

        private void OnTrackRecommend (object sender, EventArgs args)
        {
        }

#endregion

        private string artist;
        public string CurrentArtist {
            get { return artist; }
            set { artist = value; }
        }

        private string album;
        public string CurrentAlbum {
            get { return album; }
            set { album = value; }
        }

        private string track;
        public string CurrentTrack {
            get { return track; }
            set { track = value; }
        }

        public void ShowLoginDialog ()
        {
            try {
                Banshee.Preferences.Gui.PreferenceDialog dialog = new Banshee.Preferences.Gui.PreferenceDialog ();
                dialog.ShowSourcePageId (lastfm.PreferencesPageId);
                dialog.Run ();
                dialog.Destroy ();
            } catch (ApplicationException) {
            }
        }

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

            this["LastfmConnectAction"].Visible = lastfm.Connection.State == ConnectionState.Disconnected;

            updating = false;
        }
    }
}
