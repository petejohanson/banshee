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
using System.Linq;
using System.Collections.Generic;

using Mono.Unix;

using Hyena;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Collection;
using Banshee.MediaEngine;
using Banshee.ServiceStack;

namespace Banshee.Collection.Database
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
        public TimeSpan Position { get; set; }

        [DatabaseColumn]
        public DateTime CreatedAt { get; set; }

        [DatabaseColumn]
        public string Type { get; set; }

        public string Name {
            get {
                int position_seconds = (int)Position.TotalSeconds;
                return String.Format (NAME_FMT,
                    Track.DisplayTrackTitle, position_seconds / 60, position_seconds % 60
                );
            }
        }

        public DatabaseTrackInfo Track { get; set; }

        public Bookmark () {}

        public Bookmark (DatabaseTrackInfo track, int position_ms) : this (track, position_ms, null) {}

        public Bookmark (DatabaseTrackInfo track, int position_ms, string type)
        {
            Track = track;
            Position = TimeSpan.FromMilliseconds (position_ms);
            CreatedAt = DateTime.Now;
            Type = type;

            Save ();
        }

        public void Save ()
        {
            Provider.Save (this);
        }

        public void JumpTo ()
        {
            var track = Track;
            var current_track = ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo;

            if (track == null) {
                Log.ErrorFormat ("Tried to jump to bookmark {0}, but track is null", BookmarkId);
                Remove ();
            }

            if (current_track == null || current_track.TrackId != track.TrackId) {
                // Not already playing this track, so load it up
                //Console.WriteLine ("JumpTo: not already playing, loading it up");
                ServiceManager.PlayerEngine.ConnectEvent (HandleStateChanged, PlayerEvent.StateChange);
                ServiceManager.PlayerEngine.Open (track);
            } else {
                // Already playing this track, so just seek to the right position
                if (ServiceManager.PlayerEngine.CanSeek) {
                    //Console.WriteLine ("JumpTo: already playing, can seek; setting Position");
                    ServiceManager.PlayerEngine.Position = (uint)Position.TotalMilliseconds;
                } else {
                    //Console.WriteLine ("JumpTo: already playing, cannot seek");
                    ServiceManager.PlayerEngine.ConnectEvent (HandleStateChanged, PlayerEvent.StateChange);
                    ServiceManager.PlayerEngine.Play ();
                }
            }
        }

        private void HandleStateChanged (PlayerEventArgs args)
        {
            var state = ((PlayerEventStateChangeArgs)args).Current;
            /*Console.WriteLine ("JumpTo: HandleStateChanged, state is {0} Can seek? {1}  Position {2}",
                state, ServiceManager.PlayerEngine.CanSeek, ServiceManager.PlayerEngine.Position
            );*/

            bool jumped = false;
            if (state == PlayerState.Loaded || state == PlayerState.Playing) {
                if (!ServiceManager.PlayerEngine.CurrentTrack.IsLive) {
                    // Sleep in 5ms increments for at most 250ms waiting for CanSeek to be true
                    int count = 0;
                    while (count < 50 && !ServiceManager.PlayerEngine.CanSeek) {
                        //Console.WriteLine ("JumpTo: HandleStateChanged, can't seek yet, waiting 5 ms");
                        System.Threading.Thread.Sleep (5);
                        count++;
                    }
                }

                if (ServiceManager.PlayerEngine.CanSeek) {
                    //Console.WriteLine ("JumpTo: HandleStateChanged, can seek - jumping!");
                    ServiceManager.PlayerEngine.Position = (uint)Position.TotalMilliseconds;
                    jumped = true;
                } else {
                    //Console.WriteLine ("JumpTo: HandleStateChanged, can't seek - bailing :(");
                }
            }

            if (jumped || state == PlayerState.Playing) {
                ServiceManager.PlayerEngine.DisconnectEvent (HandleStateChanged);
            }
        }

        public void Remove ()
        {
            Provider.Delete (this);
        }

        // Translators: This is used to generate bookmark names. {0} is track title, {1} is minutes
        // (possibly more than two digits) and {2} is seconds (between 00 and 60).
        private static readonly string NAME_FMT = Catalog.GetString ("{0} ({1}:{2:00})");
    }
}
