//
// Bookmark.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008-2010 Novell, Inc.
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
using System.Data;
using System.Linq;
using System.Collections.Generic;

using Gtk;
using Mono.Unix;

using Hyena;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.MediaEngine;
using Banshee.Gui;
using Banshee.ServiceStack;

namespace Banshee.Bookmarks
{
    public class Bookmark
    {
        private static SqliteModelProvider<Bookmark> provider;
        public static SqliteModelProvider<Bookmark> Provider {
            get {
                return provider ?? (provider = new SqliteModelProvider<Bookmark> (ServiceManager.DbConnection, "Bookmarks", true));
            }
        }

        [DatabaseColumn (Constraints = DatabaseColumnConstraints.PrimaryKey)]
        public long BookmarkId { get; private set; }

        [DatabaseColumn]
        protected int TrackId {
            get { return Track.TrackId; }
            set {
                Track = DatabaseTrackInfo.Provider.FetchSingle (value);
            }
        }

        [DatabaseColumn]
        public TimeSpan Position { get; private set; }

        [DatabaseColumn]
        public DateTime CreatedAt { get; private set; }

        public string Name {
            get {
                int position_seconds = (int)Position.TotalSeconds;
                return String.Format (NAME_FMT,
                    Track.DisplayTrackTitle, position_seconds / 60, position_seconds % 60
                );
            }
        }

        public DatabaseTrackInfo Track { get; private set; }

        public Bookmark () {}

        public Bookmark (DatabaseTrackInfo track, int position_ms)
        {
            Track = track;
            Position = TimeSpan.FromMilliseconds (position_ms);
            CreatedAt = DateTime.Now;

            Provider.Save (this);
        }

        public void JumpTo ()
        {
            var track = Track;
            var current_track = ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo;
            if (track != null) {
                if (current_track == null || current_track.TrackId != track.TrackId) {
                    ServiceManager.PlayerEngine.ConnectEvent (HandleStateChanged, PlayerEvent.StateChange);
                    ServiceManager.PlayerEngine.OpenPlay (track);
                } else {
                    if (ServiceManager.PlayerEngine.CanSeek) {
                        ServiceManager.PlayerEngine.Position = (uint)Position.TotalMilliseconds;
                    } else {
                        ServiceManager.PlayerEngine.ConnectEvent (HandleStateChanged, PlayerEvent.StateChange);
                        ServiceManager.PlayerEngine.Play ();
                    }
                }
            } else {
                Remove ();
            }
        }

        private void HandleStateChanged (PlayerEventArgs args)
        {
            if (((PlayerEventStateChangeArgs)args).Current == PlayerState.Playing) {
                ServiceManager.PlayerEngine.DisconnectEvent (HandleStateChanged);

                if (!ServiceManager.PlayerEngine.CurrentTrack.IsLive) {
                    // Sleep in 5ms increments for at most 250ms waiting for CanSeek to be true
                    int count = 0;
                    while (count < 50 && !ServiceManager.PlayerEngine.CanSeek) {
                        System.Threading.Thread.Sleep (5);
                        count++;
                    }
                }

                if (ServiceManager.PlayerEngine.CanSeek) {
                    ServiceManager.PlayerEngine.Position = (uint)Position.TotalMilliseconds;
                }
            }
        }

        public void Remove ()
        {
            try {
                Provider.Delete (this);

                if (BookmarkUI.Instantiated) {
                    BookmarkUI.Instance.RemoveBookmark (this);
                }
            } catch (Exception e) {
                Log.Exception ("Error Removing Bookmark", e);
            }
        }

        public static List<Bookmark> LoadAll ()
        {
            return Provider.FetchAll ().ToList ();
        }

        // Translators: This is used to generate bookmark names. {0} is track title, {1} is minutes
        // (possibly more than two digits) and {2} is seconds (between 00 and 60).
        private static readonly string NAME_FMT = Catalog.GetString ("{0} ({1}:{2:00})");
    }
}
