//
// AppleDeviceSource.cs
//
// Author:
//   Alan McGovern <amcgovern@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
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
using Banshee.Base;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Library;
using System.Collections.Generic;
using System.Threading;
using Banshee.Hardware;
using Banshee.Sources;
using Banshee.I18n;
using Hyena;

namespace Banshee.Dap.AppleDevice
{
    public class AppleDeviceSource : DapSource
    {
        GPod.Device Device {
            get; set;
        }

        IVolume Volume {
            get; set;
        }

        GPod.ITDB MediaDatabase {
            get; set;
        }

        private Dictionary<int, AppleDeviceTrackInfo> tracks_map = new Dictionary<int, AppleDeviceTrackInfo> (); // FIXME: EPIC FAIL

#region Device Setup/Dispose

        public override void DeviceInitialize (IDevice device)
        {
            Volume = device as IVolume;

            if (Volume == null) {
                throw new InvalidDeviceException ();
            }

            Device = new GPod.Device (Volume.MountPoint);

            if (GPod.ITDB.GetControlPath (Device) == null) {
                throw new InvalidDeviceException ();
            }

            base.DeviceInitialize (device);

            Name = Volume.Name;
            SupportsPlaylists = true;
            SupportsPodcasts = Device.SupportsPodcast;
            SupportsVideo = Device.SupportsVideo;

            Initialize ();

            // FIXME: Properly parse the device, color and generation and don't use the fallback strings

            // IpodInfo is null on Macos formated ipods. I don't think we can really do anything with them
            // but they get loaded as UMS devices if we throw an NRE here.
            if (Device.IpodInfo != null) {
                AddDapProperty (Catalog.GetString ("Device"), Device.IpodInfo.ModelString);
                AddDapProperty (Catalog.GetString ("Generation"), Device.IpodInfo.GenerationString);
            }

            // FIXME
            //AddDapProperty (Catalog.GetString ("Color"), "black");
            AddDapProperty (Catalog.GetString ("Capacity"), string.Format ("{0:0.00}GB", BytesCapacity));
            AddDapProperty (Catalog.GetString ("Available"), string.Format ("{0:0.00}GB", BytesAvailable));
            AddDapProperty (Catalog.GetString ("Serial number"), Volume.Serial);
            //AddDapProperty (Catalog.GetString ("Produced on"), ipod_device.ProductionInfo.DisplayDate);
            //AddDapProperty (Catalog.GetString ("Firmware"), ipod_device.FirmwareVersion);

            //string [] capabilities = new string [ipod_device.ModelInfo.Capabilities.Count];
            //ipod_device.ModelInfo.Capabilities.CopyTo (capabilities, 0);
            //AddDapProperty (Catalog.GetString ("Capabilities"), String.Join (", ", capabilities));
            AddYesNoDapProperty (Catalog.GetString ("Supports cover art"), Device.SupportsArtwork);
            AddYesNoDapProperty (Catalog.GetString ("Supports photos"), Device.SupportsPhoto);
        }

        public override void Dispose ()
        {
            //ThreadAssist.ProxyToMain (DestroyUnsupportedView);
            CancelSyncThread ();
            base.Dispose ();
        }

        // WARNING: This will be called from a thread!
        protected override void Eject ()
        {
            base.Eject ();
            CancelSyncThread ();
            if (Volume.CanUnmount)
                Volume.Unmount ();
            if (Volume.CanEject)
                Volume.Eject ();

            Dispose ();
        }

        protected override bool CanHandleDeviceCommand (DeviceCommand command)
        {
            // Whats this for?
            return false;
//            try {
//                SafeUri uri = new SafeUri (command.DeviceId);
//                return IpodDevice.MountPoint.StartsWith (uri.LocalPath);
//            } catch {
//                return false;
//            }
        }

#endregion

#region Database Loading

        // WARNING: This will be called from a thread!
        protected override void LoadFromDevice ()
        {
            LoadFromDevice (false);
            OnTracksAdded ();
        }

        private void LoadFromDevice (bool refresh)
        {
            tracks_map.Clear ();
            if (refresh || MediaDatabase  == null) {
                if (MediaDatabase != null)
                    MediaDatabase.Dispose ();

                MediaDatabase = new GPod.ITDB (Device.Mountpoint);
            }

            foreach (var ipod_track in MediaDatabase.Tracks) {
                try {
                    var track = new AppleDeviceTrackInfo (ipod_track);
                    if (!tracks_map.ContainsKey (track.TrackId)) {
                        track.PrimarySource = this;
                        track.Save (false);
                        tracks_map.Add (track.TrackId, track);
                    }
                } catch (Exception e) {
                    Log.Exception (e);
                }
            }

//                Hyena.Data.Sqlite.HyenaSqliteCommand insert_cmd = new Hyena.Data.Sqlite.HyenaSqliteCommand (
//                    @"INSERT INTO CorePlaylistEntries (PlaylistID, TrackID)
//                        SELECT ?, TrackID FROM CoreTracks WHERE PrimarySourceID = ? AND ExternalID = ?");
//                foreach (IPod.Playlist playlist in ipod_device.TrackDatabase.Playlists) {
//                    if (playlist.IsOnTheGo) { // || playlist.IsPodcast) {
//                        continue;
//                    }
//                    PlaylistSource pl_src = new PlaylistSource (playlist.Name, this);
//                    pl_src.Save ();
//                    // We use the IPod.Track.Id here b/c we just shoved it into ExternalID above when we loaded
//                    // the tracks, however when we sync, the Track.Id values may/will change.
//                    foreach (IPod.Track track in playlist.Tracks) {
//                        ServiceManager.DbConnection.Execute (insert_cmd, pl_src.DbId, this.DbId, track.Id);
//                    }
//                    pl_src.UpdateCounts ();
//                    AddChildSource (pl_src);
//                }
            /*else {
                BuildDatabaseUnsupportedWidget ();
            }*/

            /*if(previous_database_supported != database_supported) {
                OnPropertiesChanged();
            }*/
        }

//        private void DestroyUnsupportedView ()
//        {
//            if (unsupported_view != null) {
//                unsupported_view.Refresh -= OnRebuildDatabaseRefresh;
//                unsupported_view.Destroy ();
//                unsupported_view = null;
//            }
//        }

#endregion

#region Source Cosmetics

        internal string [] _GetIconNames ()
        {
            return GetIconNames ();
        }

        protected override string [] GetIconNames ()
        {
            string [] names = new string[4];
            string prefix = "multimedia-player-";
            //string shell_color = "green";

            names[0] = "";
            names[2] = "ipod-standard-color";
            names[3] = "multimedia-player";
            /*
            switch ("grayscale") {
                case "grayscale":
                    names[1] = "ipod-standard-monochrome";
                    break;
                case "color":
                    names[1] = "ipod-standard-color";
                    break;
                case "mini":
                    names[1] = String.Format ("ipod-mini-{0}", shell_color);
                    names[2] = "ipod-mini-silver";
                    break;
                case "shuffle":
                    names[1] = String.Format ("ipod-shuffle-{0}", shell_color);
                    names[2] = "ipod-shuffle";
                    break;
                case "nano":
                case "nano3":
                    names[1] = String.Format ("ipod-nano-{0}", shell_color);
                    names[2] = "ipod-nano-white";
                    break;
                case "video":
                    names[1] = String.Format ("ipod-video-{0}", shell_color);
                    names[2] = "ipod-video-white";
                    break;
                case "classic":
                case "touch":
                case "phone":
                default:
                    break;
            }
          */
            names[1] = names[1] ?? names[2];
            names[1] = prefix + names[1];
            names[2] = prefix + names[2];

            return names;
        }

        public override void Rename (string name)
        {
            return;
//            if (!CanRename) {
//                return;
//            }
//
//            try {
//                if (name_path != null) {
//                    Directory.CreateDirectory (Path.GetDirectoryName (name_path));
//
//                    using (StreamWriter writer = new StreamWriter (File.Open (name_path, FileMode.Create),
//                        System.Text.Encoding.Unicode)) {
//                        writer.Write (name);
//                    }
//                }
//            } catch (Exception e) {
//                Log.Exception (e);
//            }
//
//            ipod_device.Name = name;
//            base.Rename (name);
        }

        public override bool CanRename {
            get { return !(IsAdding || IsDeleting || IsReadOnly); }
        }

        public override long BytesUsed {
            get { return (long) Volume.Capacity - Volume.Available; }
        }

        public override long BytesCapacity {
            get { return (long) Volume.Capacity; }
        }

#endregion

#region Syncing

        protected override void OnTracksAdded ()
        {
            if (!IsAdding && tracks_to_add.Count > 0 && !Sync.Syncing) {
                QueueSync ();
            }
            base.OnTracksAdded ();
        }

        protected override void OnTracksDeleted ()
        {
            if (!IsDeleting && tracks_to_remove.Count > 0 && !Sync.Syncing) {
                QueueSync ();
            }
            base.OnTracksDeleted ();
        }

        private Queue<AppleDeviceTrackInfo> tracks_to_add = new Queue<AppleDeviceTrackInfo> ();
        private Queue<AppleDeviceTrackInfo> tracks_to_remove = new Queue<AppleDeviceTrackInfo> ();

        private uint sync_timeout_id = 0;
        private object sync_timeout_mutex = new object ();
        private object sync_mutex = new object ();
        private Thread sync_thread;
        private AutoResetEvent sync_thread_wait;
        private bool sync_thread_dispose = false;

        public override bool AcceptsInputFromSource (Source source)
        {
            return base.AcceptsInputFromSource (source);
        }
        public override bool CanAddTracks {
            get {
                return base.CanAddTracks;
            }
        }
        public override bool IsReadOnly {
            get { return false; }//!database_supported; }
        }

        public override void Import ()
        {
            Banshee.ServiceStack.ServiceManager.Get<LibraryImportManager> ().Enqueue (GPod.ITDB.GetMusicPath (Device));
        }

        public override void CopyTrackTo (DatabaseTrackInfo track, SafeUri uri, BatchUserJob job)
        {
            Banshee.IO.File.Copy (track.Uri, uri, false);
        }

        protected override bool DeleteTrack (DatabaseTrackInfo track)
        {
            lock (sync_mutex) {
                if (!tracks_map.ContainsKey (track.TrackId)) {
                    return true;
                }

                var ipod_track = tracks_map[track.TrackId];
                if (ipod_track != null) {
                    tracks_to_remove.Enqueue (ipod_track);
                }

                return true;
            }
        }

        protected override void AddTrackToDevice (DatabaseTrackInfo track, SafeUri fromUri)
        {
            lock (sync_mutex) {
                if (track.PrimarySourceId == DbId) {
                    return;
                }

                if (track.Duration.Equals (TimeSpan.Zero)) {
                    throw new Exception (Catalog.GetString ("Track duration is zero"));
                }

                AppleDeviceTrackInfo ipod_track = new AppleDeviceTrackInfo (track);
                ipod_track.Uri = fromUri;
                ipod_track.PrimarySource = this;
                ipod_track.Save (false);

                tracks_to_add.Enqueue (ipod_track);
            }
        }

        public override void SyncPlaylists ()
        {
            if (!IsReadOnly && Monitor.TryEnter (sync_mutex)) {
                PerformSync ();
                Monitor.Exit (sync_mutex);
            }
        }

        private void QueueSync ()
        {
            lock (sync_timeout_mutex) {
                if (sync_timeout_id > 0) {
                    Application.IdleTimeoutRemove (sync_timeout_id);
                }

                sync_timeout_id = Application.RunTimeout (150, PerformSync);
            }
        }

        private void CancelSyncThread ()
        {
            Thread thread = sync_thread;
            lock (sync_mutex) {
                if (sync_thread != null && sync_thread_wait != null) {
                    sync_thread_dispose = true;
                    sync_thread_wait.Set ();
                }
            }

            if (thread != null) {
                thread.Join ();
            }
        }

        private bool PerformSync ()
        {
            lock (sync_mutex) {
                if (sync_thread == null) {
                    sync_thread_wait = new AutoResetEvent (false);

                    sync_thread = new Thread (new ThreadStart (PerformSyncThread));
                    sync_thread.Name = "iPod Sync Thread";
                    sync_thread.IsBackground = false;
                    sync_thread.Priority = ThreadPriority.Lowest;
                    sync_thread.Start ();
                }

                sync_thread_wait.Set ();

                lock (sync_timeout_mutex) {
                    sync_timeout_id = 0;
                }

                return false;
            }
        }

        private void PerformSyncThread ()
        {
            try {
                while (true) {
                    sync_thread_wait.WaitOne ();
                    if (sync_thread_dispose) {
                        break;
                    }

                    PerformSyncThreadCycle ();
                }

                lock (sync_mutex) {
                    sync_thread_dispose = false;
                    sync_thread_wait.Close ();
                    sync_thread_wait = null;
                    sync_thread = null;
                }
            } catch (Exception e) {
                Log.Exception (e);
            }
        }

        private void PerformSyncThreadCycle ()
        {
            OnIpodDatabaseSaveStarted (this, EventArgs.Empty);
            while (tracks_to_add.Count > 0) {
                AppleDeviceTrackInfo track = null;
                lock (sync_mutex) {
                    track = tracks_to_add.Dequeue ();
                }

                try {
                    OnIpodDatabaseSaveProgressChanged (this, EventArgs.Empty);
                    track.CommitToIpod (MediaDatabase);
                    tracks_map[track.TrackId] = track;
                } catch (Exception e) {
                    Log.Exception ("Cannot save track to iPod", e);
                }
            }

            // TODO sync updated metadata to changed tracks

            while (tracks_to_remove.Count > 0) {
                AppleDeviceTrackInfo track = null;
                lock (sync_mutex) {
                    track = tracks_to_remove.Dequeue ();
                }

                if (tracks_map.ContainsKey (track.TrackId)) {
                    tracks_map.Remove (track.TrackId);
                }

                try {
                    if (track.IpodTrack != null) {
                        OnIpodDatabaseSaveProgressChanged (this, EventArgs.Empty);
                        foreach (var playlist in MediaDatabase.Playlists)
                            playlist.Tracks.Remove (track.IpodTrack);
                        MediaDatabase.MasterPlaylist.Tracks.Remove (track.IpodTrack);
                        MediaDatabase.Tracks.Remove (track.IpodTrack);
                        Banshee.IO.File.Delete (new SafeUri (GPod.ITDB.GetLocalPath (Device, track.IpodTrack)));
                    } else {
                        Log.Error ("The ipod track was null");
                    }
                } catch (Exception e) {
                    Log.Exception ("Cannot remove track from iPod", e);
                }
            }

//            // Remove playlists on the device
//            List<IPod.Playlist> device_playlists = new List<IPod.Playlist> (ipod_device.TrackDatabase.Playlists);
//            foreach (IPod.Playlist playlist in device_playlists) {
//                if (!playlist.IsOnTheGo) {
//                    ipod_device.TrackDatabase.RemovePlaylist (playlist);
//                }
//            }
//            device_playlists.Clear ();
//
//            if (SupportsPlaylists) {
//                // Add playlists from Banshee to the device
//                foreach (Source child in Children) {
//                    PlaylistSource from = child as PlaylistSource;
//                    if (from != null && from.Count > 0) {
//                        IPod.Playlist playlist = ipod_device.TrackDatabase.CreatePlaylist (from.Name);
//                        foreach (int track_id in ServiceManager.DbConnection.QueryEnumerable<int> (String.Format (
//                            "SELECT CoreTracks.TrackID FROM {0} WHERE {1}",
//                            from.DatabaseTrackModel.ConditionFromFragment, from.DatabaseTrackModel.Condition)))
//                        {
//                            if (tracks_map.ContainsKey (track_id)) {
//                                playlist.AddTrack (tracks_map[track_id].IpodTrack);
//                            }
//                        }
//                    }
//                }
//            }

            try {
//                ipod_device.TrackDatabase.SaveStarted += OnIpodDatabaseSaveStarted;
//                ipod_device.TrackDatabase.SaveEnded += OnIpodDatabaseSaveEnded;
//                ipod_device.TrackDatabase.SaveProgressChanged += OnIpodDatabaseSaveProgressChanged;
                MediaDatabase.Write ();
                Log.Information ("Wrote iPod database");
            } catch (Exception e) {
                Log.Exception ("Failed to save iPod database", e);
            } finally {
                OnIpodDatabaseSaveEnded (this, EventArgs.Empty);
//                ipod_device.TrackDatabase.SaveStarted -= OnIpodDatabaseSaveStarted;
//                ipod_device.TrackDatabase.SaveEnded -= OnIpodDatabaseSaveEnded;
//                ipod_device.TrackDatabase.SaveProgressChanged -= OnIpodDatabaseSaveProgressChanged;
            }
        }

        private UserJob sync_user_job;

        private void OnIpodDatabaseSaveStarted (object o, EventArgs args)
        {
            DisposeSyncUserJob ();

            sync_user_job = new UserJob (Catalog.GetString ("Syncing iPod"),
                Catalog.GetString ("Preparing to synchronize..."), GetIconNames ());
            sync_user_job.Register ();
        }

        private void OnIpodDatabaseSaveEnded (object o, EventArgs args)
        {
            DisposeSyncUserJob ();
        }

        private void DisposeSyncUserJob ()
        {
            if (sync_user_job != null) {
                sync_user_job.Finish ();
                sync_user_job = null;
            }
        }

        private void OnIpodDatabaseSaveProgressChanged (object o, EventArgs args)
        {
            string message = string.Format ("Copying track {0} of {1}", 1, tracks_to_add.Count + tracks_to_remove.Count);
            double progress = 1.0 / (tracks_to_add.Count + tracks_to_remove.Count);
             if (progress >= 0.99) {
                 sync_user_job.Status = Catalog.GetString ("Flushing to disk...");
                 sync_user_job.Progress = 0;
             } else {
                 sync_user_job.Status = message;
                 sync_user_job.Progress = progress;
             }
        }

        public bool SyncNeeded {
            get {
                lock (sync_mutex) {
                    return tracks_to_add.Count > 0 || tracks_to_remove.Count > 0;
                }
            }
        }
#endregion

    }
}
