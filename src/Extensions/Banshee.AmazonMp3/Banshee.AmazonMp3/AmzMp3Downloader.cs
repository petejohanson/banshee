// 
// AmzMp3Downloader.cs
//  
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;

using Hyena;
using Hyena.Downloader;
using Xspf = Media.Playlists.Xspf;

namespace Banshee.AmazonMp3
{
    public class AmzMp3Downloader : HttpFileDownloader
    {
        private static string amazon_mp3_downloader_compat_version = "1.0.9";
        public static string AmazonMp3DownloaderCompatVersion {
            get { return amazon_mp3_downloader_compat_version; }
        }

        public Xspf.Track Track { get; private set; }
        public string OutputPath { get; set; }

        public AmzMp3Downloader (Xspf.Track track)
        {
            UserAgent = String.Format ("Amazon MP3 Downloader (Linux {0} en_US)", AmazonMp3DownloaderCompatVersion);
            TempPathRoot = Path.Combine (Path.GetTempPath (), "banshee-amz-downloader");
            Uri = track.Locations[0];
            Track = track;
            Name = String.Format ("{0} ({1})", Track.Title, Track.Creator);

            var meta = track.FindMetaEntry (new Uri ("http://www.amazon.com/dmusic/trackType"));
            FileExtension = !meta.Equals (Xspf.MetaEntry.Zero) ? meta.Value : "mp3";
        }

        protected override void OnFileFinished ()
        {
            if (!State.Success) {
                return;
            }

            base.OnFileFinished ();

            if (OutputPath == null) {
                return;
            }

            string track_dir = null;
            string track_path = null;

            if (FileExtension == "mp3") {
                using (var file = TagLib.File.Create (LocalPath, "taglib/mp3", TagLib.ReadStyle.Average)) {
                    var artist = StringUtil.EscapeFilename (file.Tag.JoinedPerformers);
                    var album = StringUtil.EscapeFilename (file.Tag.Album);
                    var title = StringUtil.EscapeFilename (file.Tag.Title);

                    track_dir = Path.Combine (OutputPath, Path.Combine (artist, album));
                    track_path = Path.Combine (track_dir, String.Format ("{0:00}. {1}.mp3",
                        file.Tag.Track, title));
                }
            } else {
                track_dir = Path.Combine (OutputPath, Path.Combine (Track.Creator, Track.Album));
                track_path = Path.Combine (track_dir, Track.Title + "." + FileExtension);
            }

            Directory.CreateDirectory (track_dir);
            File.Copy (LocalPath, track_path, true);
            File.Delete (LocalPath);
        }
    }
}
