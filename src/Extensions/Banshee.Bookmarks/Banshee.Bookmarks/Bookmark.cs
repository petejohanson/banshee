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
        private int id;
        private int track_id;
        private uint position;
        private DateTime created_at;

        // Translators: This is used to generate bookmark names. {0} is track title, {1} is minutes
        // (possibly more than two digits) and {2} is seconds (between 00 and 60).
        private readonly string bookmark_format = Catalog.GetString("{0} ({1}:{2:00})");

        private string name;
        public string Name {
            get { return name; }
            set { name = value; }
        }

        public DateTime CreatedAt {
            get { return created_at; }
        }

        public TrackInfo Track {
            get { return DatabaseTrackInfo.Provider.FetchSingle(track_id); }
        }

        private Bookmark(int id, int track_id, uint position, DateTime created_at)
        {
            this.id = id;
            this.track_id = track_id;
            this.position = position;
            this.created_at = created_at;
            uint position_seconds = position/1000;
            Name = String.Format(bookmark_format, Track.DisplayTrackTitle, position_seconds/60, position_seconds%60);
        }

        public Bookmark(int track_id, uint position)
        {
            Console.WriteLine ("Bookmark, main thread? {0}", ThreadAssist.InMainThread);
            this.track_id = track_id;
            this.position = position;
            this.created_at = DateTime.Now;
            uint position_seconds = position/1000;
            Name = String.Format(bookmark_format, Track.DisplayTrackTitle, position_seconds/60, position_seconds%60);

            this.id = ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (@"
                    INSERT INTO Bookmarks
                    (TrackID, Position, CreatedAt)
                    VALUES (?, ?, ?)",
                    track_id, position, DateTimeUtil.FromDateTime(created_at) ));
        }

        public void JumpTo()
        {
            DatabaseTrackInfo track = Track as DatabaseTrackInfo;
            DatabaseTrackInfo current_track = ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo;
            if (track != null) {
                if (current_track == null || current_track.TrackId != track.TrackId) {
                    ServiceManager.PlayerEngine.ConnectEvent (HandleStateChanged, PlayerEvent.StateChange);
                    ServiceManager.PlayerEngine.OpenPlay (track);
                } else {
                    if (ServiceManager.PlayerEngine.CanSeek) {
                        ServiceManager.PlayerEngine.Position = position;
                    } else {
                        ServiceManager.PlayerEngine.ConnectEvent (HandleStateChanged, PlayerEvent.StateChange);
                        ServiceManager.PlayerEngine.Play ();
                    }
                }
            } else {
                Remove();
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
                    ServiceManager.PlayerEngine.Position = position;
                }
            }
        }

        public void Remove()
        {
            try {
                ServiceManager.DbConnection.Execute(String.Format(
                    "DELETE FROM Bookmarks WHERE BookmarkID = {0}", id
                ));

                if (BookmarkUI.Instantiated)
                    BookmarkUI.Instance.RemoveBookmark(this);
            } catch (Exception e) {
                Log.Warning("Error Removing Bookmark", e.ToString(), false);
            }
        }

        public static List<Bookmark> LoadAll()
        {
            List<Bookmark> bookmarks = new List<Bookmark>();

            IDataReader reader = ServiceManager.DbConnection.Query(
                "SELECT BookmarkID, TrackID, Position, CreatedAt FROM Bookmarks"
            );

            while (reader.Read()) {
                try {
                    bookmarks.Add(new Bookmark(
                        reader.GetInt32 (0), reader.GetInt32 (1), Convert.ToUInt32 (reader[2]),
                        DateTimeUtil.ToDateTime(Convert.ToInt64(reader[3]))
                    ));
                } catch (Exception e) {
                    ServiceManager.DbConnection.Execute(String.Format(
                        "DELETE FROM Bookmarks WHERE BookmarkID = {0}", reader.GetInt32 (0)
                    ));

                    Log.Warning("Error Loading Bookmark", e.ToString(), false);
                }
            }
            reader.Dispose();

            return bookmarks;
        }

        public static void Initialize()
        {
            if (!ServiceManager.DbConnection.TableExists("Bookmarks")) {
                ServiceManager.DbConnection.Execute(@"
                    CREATE TABLE Bookmarks (
                        BookmarkID          INTEGER PRIMARY KEY,
                        TrackID             INTEGER NOT NULL,
                        Position            INTEGER NOT NULL,
                        CreatedAt           INTEGER NOT NULL
                    )
                ");
            }
        }
    }
}
