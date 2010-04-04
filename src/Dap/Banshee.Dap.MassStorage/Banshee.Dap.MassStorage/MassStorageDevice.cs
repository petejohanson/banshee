//
// MassStorageDevice.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Mono.Unix;

using Hyena;

using Banshee.Base;
using Banshee.Hardware;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.Dap.MassStorage
{
    public class MassStorageDevice : IDeviceMediaCapabilities
    {
        private MassStorageSource source;
        public MassStorageSource Source {
            get { return source; }
            set { source = value; }
        }

        public MassStorageDevice ()
        {
        }

        public MassStorageDevice (MassStorageSource source)
        {
            Source = source;
        }

        public virtual void SourceInitialize ()
        {
        }

        public virtual bool DeleteTrackHook (DatabaseTrackInfo track)
        {
            return true;
        }

        public virtual bool ShouldIgnoreDevice ()
        {
            return File.Exists (IsNotAudioPlayerPath);
        }

        public virtual bool LoadDeviceConfiguration ()
        {
            string path = IsAudioPlayerPath;
            string path_rockbox = System.IO.Path.Combine (source.Volume.MountPoint, ".rockbox/config.cfg");

            if (!File.Exists (path) && !File.Exists (path_rockbox) ) {
                return false;
            }

            if (File.Exists (path_rockbox) ) {
                Hyena.Log.DebugFormat ("Found RockBox Device");
                name = Catalog.GetString ("Rockbox Device");
                audio_folders = new string [] {"Music/","Videos/"};
                video_folders = new string [] {"Videos/"};
                folder_depth = 2;
                playback_mime_types = new string [] {"application/ogg","audio/x-ms-wma","audio/mpeg","audio/mp4","audio/flac","audio/aac","audio/mp4","audio/x-wav"};
                playlist_formats = new string [] {"audio/x-mpegurl"};
                playlist_path = "Playlists/";
                cover_art_file_name = "cover.jpg";
                cover_art_file_type = "jpeg";
                cover_art_size = 320;

            } else {
                LoadConfig ();
            }

            return true;
        }

        protected void LoadConfig ()
        {
            var config = new Dictionary<string, string[]> ();

            if (File.Exists (IsAudioPlayerPath)) {

                try {
                    using (var reader = new StreamReader (IsAudioPlayerPath)) {
                        config = new KeyValueParser (reader);
                        has_is_audio_player_file = true;
                    }

                } catch (Exception e) {
                    Log.Exception ("Error parsing " + IsAudioPlayerPath, e);
                }
            }

            name = GetPreferredValue ("name", config, DefaultName);
            cover_art_file_type = GetPreferredValue ("cover_art_file_type", config, DefaultCoverArtFileType);
            cover_art_file_name = GetPreferredValue ("cover_art_file_name", config, DefaultCoverArtFileName);
            cover_art_size = GetPreferredValue ("cover_art_size", config, DefaultCoverArtSize);
            audio_folders = MergeValues ("audio_folders", config, DefaultAudioFolders);
            video_folders = MergeValues ("video_folders", config, DefaultVideoFolders);
            playback_mime_types = MergeValues ("output_formats", config, DefaultPlaybackMimeTypes);
            playlist_formats = MergeValues ("playlist_formats", config, DefaultPlaylistFormats);
            playlist_path = GetPreferredValue ("playlist_path", config, DefaultPlaylistPath);
            folder_depth = GetPreferredValue ("folder_depth", config, DefaultFolderDepth);
        }

        private string[] MergeValues (string key, IDictionary<string, string[]> config, string[] defaultValues)
        {
            if (config.ContainsKey (key)) {
                return config[key].Union (defaultValues).ToArray ();
            }
            return defaultValues;
        }

        private int GetPreferredValue (string key, IDictionary<string, string[]> config, int defaultValue)
        {
            int parsedValue;
            if (config.ContainsKey (key) && config[key].Length > 0
                    && int.TryParse (config[key][0], out parsedValue)) {
                return parsedValue;
            }
            return defaultValue;
        }

        private string GetPreferredValue (string key, IDictionary<string, string[]> config, string defaultValue)
        {
            if (config.ContainsKey (key)) {
                return config[key][0];
            }
            return defaultValue;
        }

        public virtual bool GetTrackPath (TrackInfo track, out string path)
        {
            path = null;
            return false;
        }

        private bool has_is_audio_player_file;
        public bool HasIsAudioPlayerFile {
            get { return has_is_audio_player_file; }
        }

        private string IsAudioPlayerPath {
            get { return System.IO.Path.Combine (source.Volume.MountPoint, ".is_audio_player"); }
        }

        private string IsNotAudioPlayerPath {
            get { return System.IO.Path.Combine (source.Volume.MountPoint, ".is_not_audio_player"); }
        }

        protected virtual string DefaultName {
            get { return source.Volume.Name; }
        }

        private string name;
        public virtual string Name {
            get { return name ?? source.Volume.Name; }
        }

        protected virtual int DefaultCoverArtSize {
            get { return 200; }
        }

        private int cover_art_size;
        public virtual int CoverArtSize {
            get { return cover_art_size; }
        }

        protected virtual int DefaultFolderDepth {
            get { return -1; }
        }

        private int folder_depth = -1;
        public virtual int FolderDepth {
            get { return folder_depth; }
        }

        protected virtual string [] DefaultAudioFolders {
            get { return new string[0]; }
        }

        private string[] audio_folders = new string[0];
        public virtual string[] AudioFolders {
            get { return audio_folders; }
        }

        protected virtual string[] DefaultVideoFolders {
            get { return new string[0]; }
        }

        private string[] video_folders = new string[0];
        public virtual string[] VideoFolders {
            get { return video_folders; }
        }

        protected virtual string DefaultCoverArtFileType {
            get { return ""; }
        }

        private string cover_art_file_type;
        public virtual string CoverArtFileType {
            get { return cover_art_file_type; }
        }

        protected virtual string DefaultCoverArtFileName {
            get { return "cover.jpg"; }
        }

        private string cover_art_file_name;
        public virtual string CoverArtFileName {
            get { return cover_art_file_name; }
        }

        protected virtual string[] DefaultPlaylistFormats {
            get { return new string[0]; }
        }

        private string[] playlist_formats;
        public virtual string[] PlaylistFormats {
            get { return playlist_formats; }
        }

        protected virtual string DefaultPlaylistPath {
            get { return null; }
        }

        private string playlist_path;
        public virtual string PlaylistPath {
            get { return playlist_path; }
        }

        protected virtual string[] DefaultPlaybackMimeTypes {
            get { return new string[0]; }
        }

        private string[] playback_mime_types;
        public virtual string[] PlaybackMimeTypes {
            get { return playback_mime_types; }
        }

        public virtual string DeviceType {
            get { return "mass-storage"; }
        }

        public virtual string [] GetIconNames ()
        {
            return null;
        }

        public bool IsType (string type)
        {
            return type == DeviceType;
        }
    }
}
