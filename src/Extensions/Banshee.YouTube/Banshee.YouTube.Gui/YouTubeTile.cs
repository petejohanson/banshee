//
// YouTubeTile.cs
//
// Authors:
//   Kevin Duffus <KevinDuffus@gmail.com>
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//
// Copyright (C) 2009 Kevin Duffus
// Copyright (C) 2010 Alexander Kojevnikov
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
using System.Text.RegularExpressions;

using Mono.Unix;

using Banshee.Base;
using Banshee.Web;
using Banshee.Collection;
using Banshee.ServiceStack;

using Hyena;

using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.YouTube;
using Google.YouTube;

using Banshee.YouTube.Data;
using Banshee.YouTube.Gui;

namespace Banshee.YouTube.Gui
{
    public class YouTubeTile : VideoStreamTile
    {
        public YouTubeTile (YouTubeTileData data)
        {
            BansheePlaybackUri = data.BansheePlaybackUri;
            BrowserPlaybackUri = data.BrowserPlaybackUri;
            Title = data.Title;
            Uploader = data.Uploader;
            RatingValue = data.RatingValue;

            if (data.Thumbnail != null) {
                Pixbuf = new Gdk.Pixbuf (data.Thumbnail);
            } else {
                Pixbuf = Banshee.Gui.IconThemeUtils.LoadIcon ("generic-artist", 48);
            }
        }
    }

    public class YouTubeTileData
    {
        public string BansheePlaybackUri { get; private set; }
        public string BrowserPlaybackUri { get; private set; }
        public string Title { get; private set; }
        public string Uploader { get; private set; }
        public int RatingValue { get; private set; }
        public string Thumbnail { get; private set; }

        public YouTubeTileData (Video video)
        {
            BansheePlaybackUri = GetPlaybackUri (video);
            BrowserPlaybackUri = video.WatchPage.AbsoluteUri;
            Title = video.Title;
            Uploader = video.Uploader;

            try {
                RatingValue = (int) Math.Round (video.RatingAverage);
            } catch (Exception e) {
                Log.DebugException (e);
            }

            try {
                DataFetch df = new DataFetch ();
                Thumbnail = df.DownloadContent (video.Thumbnails[0].Url, CacheDuration.Normal);
            } catch (Exception e) {
                Log.DebugException (e);
                Thumbnail = null;
            }
        }

        private static string GetPlaybackUri (Video yt_video)
        {
            int flv = (int)YouTubeQuery.VideoFormat.Embeddable;
            int mobile = (int)YouTubeQuery.VideoFormat.Mobile;
            const string format_param = "&fmt=18"; // Assumes user has broadband connection
            string video_id = yt_video.VideoId;
            string playback_uri = String.Empty;
            string flv_url = String.Empty;

            string t_param = GetTParam (yt_video.WatchPage.AbsoluteUri);

            if (String.IsNullOrEmpty (t_param)) {
                foreach (MediaContent m in yt_video.Media.Contents) {
                    uint content_format = Convert.ToUInt32 (m.Format);

                    if (content_format == flv) {
                        flv_url = m.Url;
                    }

                    // [RTSP] 3gp (MPEG-4 SP Video w/ AAC Audio)
                    if (content_format == mobile) {
                        playback_uri = m.Url;
                        break;
                    }
                }

                if (String.IsNullOrEmpty (playback_uri)) {
                    playback_uri = flv_url;
                }
            } else {
                playback_uri = String.Concat ("http://www.youtube.com/get_video?video_id=", video_id, "&t=", t_param, format_param);
            }

            return playback_uri;
        }

        private static string GetTParam (string yt_video_uri)
        {
            string t_param;
            DataFetch df = new DataFetch ();
            string watch_page_contents = df.GetWatchPageContents (yt_video_uri);

            if (String.IsNullOrEmpty (watch_page_contents)) {
                return null;
            }

            Regex regex = new Regex ("'SWF_ARGS'.*\"t\": \"([^\"]+)\"");
            Match match = regex.Match (watch_page_contents);

            if (!match.Success) {
                return null;
            }

            t_param = Regex.Unescape (match.Result ("$1"));
            if (t_param == null) {
                t_param = match.Result ("$1");
            }

            return t_param;
        }
    }
}
