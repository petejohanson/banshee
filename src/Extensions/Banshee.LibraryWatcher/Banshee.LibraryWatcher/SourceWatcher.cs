//
// SourceWatcher.cs
//
// Authors:
//   Christian Martellini <christian.martellini@gmail.com>
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//
// Copyright (C) 2009 Christian Martellini
// Copyright (C) 2009 Alexander Kojevnikov
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
using System.IO;
using System.Linq;
using System.Data;
using System.Threading;
using System.Collections.Generic;

using Hyena;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Library;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Streaming;

namespace Banshee.LibraryWatcher
{
    public class SourceWatcher : IDisposable
    {
        private readonly LibraryImportManager import_manager;
        private readonly LibrarySource library;
        private readonly FileSystemWatcher watcher;
        private readonly ManualResetEvent handle;
        private readonly Thread watch_thread;

        private readonly Queue<QueueItem> queue = new Queue<QueueItem> ();
        private readonly TimeSpan delay = TimeSpan.FromMilliseconds (1000);

        private bool active;
        private bool disposed;

        private class QueueItem
        {
            public DateTime When;
            public WatcherChangeTypes ChangeType;
            public string OldFullPath;
            public string FullPath;
            public string MetadataHash;
        }

        public SourceWatcher (LibrarySource library)
        {
            this.library = library;
            handle = new ManualResetEvent(false);
            string path = library.BaseDirectoryWithSeparator;
            string home = Environment.GetFolderPath (Environment.SpecialFolder.Personal) + Path.DirectorySeparatorChar;
            if (path == home) {
                throw new Exception ("Will not create LibraryWatcher for the entire home directory");
            }

            import_manager = ServiceManager.Get<LibraryImportManager> ();

            watcher = new FileSystemWatcher (path);
            watcher.IncludeSubdirectories = true;
            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnChanged;

            active = true;
            watch_thread = new Thread (new ThreadStart (Watch));
            watch_thread.Name = String.Format ("LibraryWatcher for {0}", library.Name);
            watch_thread.IsBackground = true;
            watch_thread.Start ();
        }

#region Public Methods

        public void Dispose ()
        {
            if (!disposed) {
                active = false;
                watcher.Changed -= OnChanged;
                watcher.Created -= OnChanged;
                watcher.Deleted -= OnChanged;
                watcher.Renamed -= OnChanged;

                lock (queue) {
                    queue.Clear ();
                }

                watcher.Dispose ();
                disposed = true;
            }
        }

#endregion

#region Private Methods

        private void OnChanged (object source, FileSystemEventArgs args)
        {
            var item = new QueueItem {
                When = DateTime.Now,
                ChangeType = args.ChangeType,
                FullPath = args.FullPath,
                OldFullPath = args is RenamedEventArgs ? ((RenamedEventArgs)args).OldFullPath : args.FullPath
            };

            lock (queue) {
                queue.Enqueue (item);
            }
            handle.Set ();

            if (args.ChangeType != WatcherChangeTypes.Changed) {
                Hyena.Log.DebugFormat ("Watcher: {0} {1}{2}",
                    item.ChangeType, args is RenamedEventArgs ? item.OldFullPath + " => " : "", item.FullPath);
            }
        }

        private void Watch ()
        {
            watcher.EnableRaisingEvents = true;

            while (active) {
                WatcherChangeTypes change_types = 0;
                while (queue.Count > 0) {
                    QueueItem item;
                    lock (queue) {
                        item = queue.Dequeue ();
                    }

                    int sleep =  (int) (item.When + delay - DateTime.Now).TotalMilliseconds;
                    if (sleep > 0) {
                        Hyena.Log.DebugFormat ("Watcher: sleeping {0}ms", sleep);
                        Thread.Sleep (sleep);
                    }

                    if (item.ChangeType == WatcherChangeTypes.Changed) {
                        UpdateTrack (item.FullPath);
                    } else if (item.ChangeType == WatcherChangeTypes.Created) {
                        AddTrack (item.FullPath);
                    } else if (item.ChangeType == WatcherChangeTypes.Deleted) {
                        RemoveTrack (item.FullPath);
                    } else if (item.ChangeType == WatcherChangeTypes.Renamed) {
                        RenameTrack (item.OldFullPath, item.FullPath);
                    }

                    change_types |= item.ChangeType;
                }

                if ((change_types & WatcherChangeTypes.Deleted) > 0) {
                    library.NotifyTracksDeleted ();
                }
                if ((change_types & (WatcherChangeTypes.Renamed |
                    WatcherChangeTypes.Created | WatcherChangeTypes.Changed)) > 0) {
                    library.NotifyTracksChanged ();
                }

                handle.WaitOne ();
                handle.Reset ();
            }
        }

        private void UpdateTrack (string track)
        {
            using (var reader = ServiceManager.DbConnection.Query (
                DatabaseTrackInfo.Provider.CreateFetchCommand (
                "CoreTracks.PrimarySourceID = ? AND CoreTracks.Uri = ? LIMIT 1"), library.DbId, new SafeUri (track).AbsoluteUri)) {
                if (reader.Read ()) {
                    var track_info = DatabaseTrackInfo.Provider.Load (reader);
                    if (Banshee.IO.File.GetModifiedTime (track_info.Uri) > track_info.FileModifiedStamp) {
                        using (var file = StreamTagger.ProcessUri (track_info.Uri)) {
                            StreamTagger.TrackInfoMerge (track_info, file, false);
                        }
                        track_info.LastSyncedStamp = DateTime.Now;
                        track_info.Save (false);
                    }
                }
            }
        }

        private void AddTrack (string track)
        {
            import_manager.ImportTrack (track);

            // Trigger file rename.
            string uri = new SafeUri(track).AbsoluteUri;
            HyenaSqliteCommand command = new HyenaSqliteCommand (@"
                UPDATE CoreTracks
                SET DateUpdatedStamp = LastSyncedStamp + 1
                WHERE Uri = ?", uri);
            ServiceManager.DbConnection.Execute (command);
        }

        private void RemoveTrack (string track)
        {
            string uri = new SafeUri(track).AbsoluteUri;
            const string hash_sql = @"SELECT TrackID, MetadataHash FROM CoreTracks WHERE Uri = ? LIMIT 1";
            int track_id = 0;
            string hash = null;
            using (var reader = new HyenaDataReader (ServiceManager.DbConnection.Query (hash_sql, uri))) {
                if (reader.Read ()) {
                    track_id = reader.Get<int> (0);
                    hash = reader.Get<string> (1);
                }
            }

            if (hash != null) {
                QueueItem item;
                lock (queue) {
                    item = queue.FirstOrDefault (
                        i => i.ChangeType == WatcherChangeTypes.Created && GetMetadataHash(i) == hash);
                }
                if (item != null) {
                    item.ChangeType = WatcherChangeTypes.Renamed;
                    item.OldFullPath = track;
                    return;
                }
            }

            const string delete_sql = @"
                INSERT INTO CoreRemovedTracks (DateRemovedStamp, TrackID, Uri)
                SELECT ?, TrackID, Uri FROM CoreTracks WHERE TrackID IN ({0})
                ;
                DELETE FROM CoreTracks WHERE TrackID IN ({0})";

            // If track_id is 0, it's a directory.
            HyenaSqliteCommand delete_command;
            if (track_id > 0) {
                delete_command = new HyenaSqliteCommand (String.Format (delete_sql,
                    "?"), DateTime.Now, track_id, track_id);
            } else {
                string pattern = StringUtil.EscapeLike (uri) + "/_%";
                delete_command = new HyenaSqliteCommand (String.Format (delete_sql,
                    @"SELECT TrackID FROM CoreTracks WHERE Uri LIKE ? ESCAPE '\'"), DateTime.Now, pattern, pattern);
            }

            ServiceManager.DbConnection.Execute (delete_command);
        }

        private void RenameTrack(string oldFullPath, string fullPath)
        {
            if (oldFullPath == fullPath) {
                // FIXME: bug in Mono, see bnc#322330
                return;
            }
            string old_uri = new SafeUri (oldFullPath).AbsoluteUri;
            string new_uri = new SafeUri (fullPath).AbsoluteUri;
            string pattern = StringUtil.EscapeLike (old_uri) + "%";
            HyenaSqliteCommand rename_command = new HyenaSqliteCommand (@"
                UPDATE CoreTracks
                SET Uri = REPLACE(Uri, ?, ?), DateUpdatedStamp = ?
                WHERE Uri LIKE ? ESCAPE '\'",
                old_uri, new_uri, DateTime.Now, pattern);
            ServiceManager.DbConnection.Execute (rename_command);
        }

        private string GetMetadataHash (QueueItem item)
        {
            if (item.ChangeType == WatcherChangeTypes.Created && item.MetadataHash == null) {
                var uri = new SafeUri (item.FullPath);
                if (DatabaseImportManager.IsWhiteListedFile (item.FullPath) && Banshee.IO.File.Exists (uri)) {
                    var track = new TrackInfo ();
                    using (var file = StreamTagger.ProcessUri (uri)) {
                        StreamTagger.TrackInfoMerge (track, file);
                    }
                    item.MetadataHash = track.MetadataHash;
                }
            }
            return item.MetadataHash;
        }

#endregion
    }
}
