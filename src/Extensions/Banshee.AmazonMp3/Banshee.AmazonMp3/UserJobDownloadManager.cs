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
                user_job.Status = String.Format (
                    Catalog.GetPluralString (
                        "{0} download at {1}/s ({2} pending)",
                        "{0} downloads at {1}/s ({2} pending)",
                        count),
                    count, new Hyena.Query.FileSizeQueryValue (
                        (long)Math.Round (speed)).ToUserQuery (),
                    PendingDownloadCount
                );
            }
        }

        protected override void OnDownloaderFinished (HttpDownloader downloader)
        {
            base.OnDownloaderFinished (downloader);

            if (downloader.State.Success) {
                import_manager.Enqueue (((HttpFileDownloader)downloader).LocalPath);
            }

            var track = ((AmzMp3Downloader)downloader).Track;
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
