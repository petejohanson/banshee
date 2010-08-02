//
// UserJobDownloadManager.cs
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

using Banshee.Library;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.I18n;

namespace Banshee.AmazonMp3
{
    public class UserJobDownloadManager : DownloadManager
    {
        private UserJob user_job;
        private int total_count;
        private int finished_count;
        private LibraryImportManager import_manager;

        public UserJobDownloadManager (string path)
        {
            var playlist = new AmzXspfPlaylist (path);
            total_count = playlist.DownloadableTrackCount;
            foreach (var track in playlist.DownloadableTracks) {
                QueueDownloader (new AmzMp3Downloader (track));
            }

            user_job = new UserJob (Catalog.GetString ("Amazon MP3 Purchases"),
                Catalog.GetString ("Contacting..."), "amazon-mp3-source");
            user_job.Register ();

            import_manager = new LibraryImportManager (true) {
                KeepUserJobHidden = true
            };
        }

        protected override void OnDownloaderStarted (HttpDownloader downloader)
        {
            var track = ((AmzMp3Downloader)downloader).Track;
            Log.InformationFormat ("Starting to download \"{0}\" by {1}", track.Title, track.Creator);
        }

        protected override void OnDownloaderProgress (HttpDownloader downloader)
        {
            lock (SyncRoot) {
                double weight = 1.0 / total_count;
                double progress = finished_count * weight;
                double speed = 0;
                int count = 0;
                foreach (var active_downloader in ActiveDownloaders) {
                    progress += weight * active_downloader.State.PercentComplete;
                    speed = active_downloader.State.TransferRate;
                    count++;
                }
                user_job.Progress = progress;

                var human_speed = new Hyena.Query.FileSizeQueryValue ((long)Math.Round (speed)).ToUserQuery ();
                if (PendingDownloadCount == 0) {
                    user_job.Status = String.Format (
                        Catalog.GetPluralString (
                            "{0} download at {1}/s",
                            "{0} downloads at {1}/s",
                            count),
                        count, human_speed
                    );
                } else {
                    user_job.Status = String.Format (
                        Catalog.GetPluralString (
                            "{0} download at {1}/s ({2} pending)",
                            "{0} downloads at {1}/s ({2} pending)",
                            count),
                        count, human_speed, PendingDownloadCount
                    );
                }
            }
        }

        protected override void OnDownloaderFinished (HttpDownloader downloader)
        {
            base.OnDownloaderFinished (downloader);

            var amz_downloader = (AmzMp3Downloader)downloader;
            var track = amz_downloader.Track;

            if (downloader.State.Success) {
                if (amz_downloader.FileExtension == "mp3") {
                    import_manager.Enqueue (((HttpFileDownloader)downloader).LocalPath);
                } else {
                    // FIXME: this just ensures that we download any extension content (e.g. pdf)
                    // but we do not actually import it since it's not an MP3. However, we should
                    // support the digital booklets (pdf) by keeping a special track in the
                    // database. Such a track would never be played, but manual activation would
                    // open the file in the default document viewer.
                    //
                    // Also, computing a path like this is not great, since it is possible that
                    // the booklet may not actually end up in the same album directory. We should
                    // probably wait to copy extensions until all tracks are downloaded, and we
                    // choose the most common album path out of all downloaded tracks as the final
                    // resting place for the extension content. Make sense? GOOD.
                    var base_path = ServiceManager.SourceManager.MusicLibrary.BaseDirectory;
                    var dir = Path.Combine (base_path, Path.Combine (track.Creator, track.Album));
                    var path = Path.Combine (dir, track.Title + "." + amz_downloader.FileExtension);
                    Directory.CreateDirectory (dir);
                    File.Copy (amz_downloader.LocalPath, path, true);
                    File.Delete (amz_downloader.LocalPath);
                }
            }

            Log.InformationFormat ("Finished downloading \"{0}\" by {1}", track.Title, track.Creator);

            lock (SyncRoot) {
                finished_count++;

                if (TotalDownloadCount <= 0) {
                    user_job.Finish ();
                }
            }
        }
    }
}
