//
// YouTubeData.cs
//
// Author:
//   Kevin Duffus <KevinDuffus@gmail.com>
//
// Copyright (C) 2009 Kevin Duffus
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
using System.Collections.Generic;
using System.Text;

using Mono.Unix;
using Gtk;

using Banshee.Widgets;

using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.YouTube;
using Google.GData.Extensions.MediaRss;
using Google.YouTube;

namespace Banshee.YouTube.Data
{
    public enum CacheDuration
    {
        None = 0,
        Normal,
        Infinite
    }

    public class DataCore
    {
        private Feed<Video> video_results;
        private YouTubeRequestSettings yt_request_settings;
        private YouTubeRequest yt_request;
        private const string app_name = "Banshee.YouTube";
        private const string client_id = "ytapi-KevinDuffus-BansheeMediaPlay-11toa30i-0";
        private const string developer_key = "AI39si7lcnwsjfic8V-K-Is-nwt2EqwnpZSBNGRuF-rUmcZ4WGB_pDSZxXI5uwMDePtMfJwvmYwDln625leh0IOBtuZ1DjV7vg";

        public bool InitYouTubeRequest ()
        {
#if HAVE_GDATASHARP_1_5
            yt_request_settings = new YouTubeRequestSettings (app_name, developer_key);
#else
            yt_request_settings = new YouTubeRequestSettings (app_name, client_id, developer_key);
#endif
            this.yt_request = new YouTubeRequest (yt_request_settings);

            if (this.yt_request != null && yt_request_settings != null) {
                return true;
            }

            return false;
        }

        public void PerformSearch (string searchVal)
        {
            YouTubeQuery query = new YouTubeQuery (YouTubeQuery.DefaultVideoUri);

            //order results by the number of views (most viewed first)
            query.OrderBy = "relevance";

            // perform querying with restricted content included in the results
            // query.SafeSearch could also be set to YouTubeQuery.SafeSearchValues.Moderate
            query.Query = searchVal;
            query.SafeSearch = YouTubeQuery.SafeSearchValues.None;

            this.video_results = yt_request.Get<Video> (query);
        }

        public Feed<Video> Videos {
            get { return this.video_results; }
        }
    }
}
