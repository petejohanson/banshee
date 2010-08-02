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
using System.Linq;
using System.Collections.Generic;

using Hyena;
using Hyena.Downloader;

using Banshee.Library;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.I18n;
using Banshee.Base;

namespace Banshee.AmazonMp3
{
    public class UserJobDownloadManager : DownloadManager
    {
        private UserJob user_job;
        private int total_count;
        private int finished_count;
        private LibraryImportManager import_manager;

        private int mp3_count;
        private List<TrackInfo> mp3_imported_tracks = new List<TrackInfo> ();
        private Queue<AmzMp3Downloader> non_mp3_queue = new Queue<AmzMp3Downloader> ();

        public UserJobDownloadManager (string path)
        {
            var playlist = new AmzXspfPlaylist (path);
            total_count = playlist.DownloadableTrackCount;
            foreach (var track in playlist.DownloadableTracks) {
                var downloader = new AmzMp3Downloader (track);
                if (downloader.FileExtension == "mp3") {
                    mp3_count++;
                }
                QueueDownloader (downloader);
            }

            user_job = new UserJob (Catalog.GetString ("Amazon MP3 Purchases"),
                Catalog.GetString ("Contacting..."), "amazon-mp3-source");
            user_job.Register ();

            import_manager = new LibraryImportManager (true) {
                KeepUserJobHidden = true
            };
            import_manager.ImportResult += OnImportManagerImportResult;
        }

        private static TResult MostCommon<T, TResult> (IEnumerable<T> collection, Func<T, TResult> map)
        {
            return (
                from item in collection
                group map (item) by map (item) into g
                orderby g.Count () descending
                select g.First ()
            ).First ();
        }

        private void OnImportManagerImportResult (object o, DatabaseImportResultArgs args)
        {
            mp3_imported_tracks.Add (args.Track);

            if (mp3_imported_tracks.Count != mp3_count || non_mp3_queue.Count <= 0) {
                return;
            }

            // FIXME: this is all pretty lame. Amazon doesn't have any metadata on the PDF
            // files, which is a shame. So I attempt to figure out the best common metadata
            // from the already imported tracks in the album, and then forcefully persist
            // this in the database. When Taglib# supports reading/writing PDF, we can
            // persist this back the the PDF file, and support it for importing like normal.

            var artist_name =
                MostCommon<TrackInfo, string> (mp3_imported_tracks, track => track.AlbumArtist) ??
                MostCommon<TrackInfo, string> (mp3_imported_tracks, track => track.ArtistName);
            var album_title =
                MostCommon<TrackInfo, string> (mp3_imported_tracks, track => track.AlbumTitle);
            var genre =
                MostCommon<TrackInfo, string> (mp3_imported_tracks, track => track.Genre);
            var copyright =
                MostCommon<TrackInfo, string> (mp3_imported_tracks, track => track.Copyright);
            var year =
                MostCommon<TrackInfo, int> (mp3_imported_tracks, track => track.Year);
            var track_count =
                MostCommon<TrackInfo, int> (mp3_imported_tracks, track => track.TrackCount);
            var disc_count =
                MostCommon<TrackInfo, int> (mp3_imported_tracks, track => track.DiscCount);

            while (non_mp3_queue.Count > 0) {
                var downloader = non_mp3_queue.Dequeue ();
                var track = new DatabaseTrackInfo () {
                    AlbumArtist = artist_name,
                    ArtistName = artist_name,
                    AlbumTitle = album_title,
                    TrackTitle = downloader.Track.Title,
                    TrackCount = track_count,
                    DiscCount = disc_count,
                    Year = year,
                    Genre = genre,
                    Copyright = copyright,
                    Uri = new SafeUri (downloader.LocalPath),
                    MediaAttributes = TrackMediaAttributes.ExternalResource,
                    PrimarySource = ServiceManager.SourceManager.MusicLibrary
                };

                track.CopyToLibraryIfAppropriate (true);

                if (downloader.FileExtension == "pdf") {
                    track.MimeType = "application/pdf";
                    Application.Invoke (delegate {
                        ServiceManager.DbConnection.BeginTransaction ();
                        try {
                            track.Save ();
                            ServiceManager.DbConnection.CommitTransaction ();
                        } catch {
                            ServiceManager.DbConnection.RollbackTransaction ();
                            throw;
                        }
                    });
                }
            }
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
                    import_manager.Enqueue (amz_downloader.LocalPath);
                } else {
                    non_mp3_queue.Enqueue (amz_downloader);
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
