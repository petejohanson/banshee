//
// DataFetch.cs
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
using System.IO;
using System.Net;
using System.Web;
using System.Collections;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.GZip;

using Banshee.Base;
using Hyena;

namespace Banshee.YouTube.Data
{
    public class DataFetch
    {
        private const int CACHE_VERSION = 2;
        public static string UserAgent = Banshee.Web.Browser.UserAgent;
        public static string CachePath = Path.Combine (Paths.ExtensionCacheRoot, "youtube");
        public static TimeSpan NormalCacheTime = TimeSpan.FromHours (2);

        private static bool initialized = false;

        public DataFetch ()
        {
            if (!initialized) {
                initialized = true;

                if (CachePath == null || UserAgent == null) {
                    throw new NotSupportedException ("CachePath and/or Useragent are null. Applications must set this value.");
                }

                VerifyCachePath ();
            }
        }

        public string GetWatchPageContents (string url)
        {
            string contents = String.Empty;

            HttpWebRequest request = (HttpWebRequest) WebRequest.Create (url);

            try {
                using (var response = (HttpWebResponse) request.GetResponse ()) {
                    using (var stream = response.GetResponseStream ()) {
                        using (var stream_reader = new StreamReader (stream)) {
                            contents = stream_reader.ReadToEnd ();
                        }
                    }
                }

                return contents;
            } catch (Exception e) {
                Log.DebugException (e);
            }

            return contents;
        }

        public string DownloadContent (string data_url)
        {
            return DownloadContent (data_url, CacheDuration.Infinite);
        }

        public string DownloadContent (string data_url, CacheDuration cache_duration)
        {
            string [] split_data_url = data_url.Split ('/');
            string fname = split_data_url[split_data_url.Length - 2] + Path.GetExtension (data_url);

            return DownloadContent (data_url, GetCachedPath (fname), cache_duration);
        }

        internal static string DownloadContent (string data_url, string cache_file, CacheDuration cache_duration)
        {
            SafeUri uri = new SafeUri (cache_file);

            if (String.IsNullOrEmpty (data_url) || String.IsNullOrEmpty (cache_file)) {
                return null;
            }

            // See if we have a valid cached copy
            if (cache_duration != CacheDuration.None) {
                if (Banshee.IO.File.Exists (uri)) {
                    DateTime last_updated_time = DateTime.FromFileTime (Banshee.IO.File.GetModifiedTime (uri));
                    if (cache_duration == CacheDuration.Infinite || DateTime.Now - last_updated_time < DataFetch.NormalCacheTime) {
                        return cache_file;
                    }
                }
            }

            HttpWebRequest request = (HttpWebRequest) WebRequest.Create (data_url);
            request.UserAgent = DataFetch.UserAgent;
            request.KeepAlive = false;

            try {
                using (HttpWebResponse response = (HttpWebResponse) request.GetResponse ()) {
                    Banshee.IO.StreamAssist.Save (GetResponseStream (response),
                                                  Banshee.IO.File.OpenWrite (uri, true));
                }
            } catch (Exception e) {
                Log.DebugException (e);
            }
            return cache_file;
        }

        private static string GetCachedPath (string fname)
        {
            if (fname == null) {
                return null;
            }

            if (fname.Length > 2) {
                return Path.Combine (DataFetch.CachePath, fname);
            } else {
                return String.Empty;
            }
        }

        private static Stream GetResponseStream (HttpWebResponse response)
        {
            return response.ContentEncoding == "gzip"
                ? new GZipInputStream (response.GetResponseStream ())
                : response.GetResponseStream ();
        }

        private static void VerifyCachePath ()
        {
            if (!Banshee.IO.Directory.Exists (CachePath)) {
                Banshee.IO.Directory.Create (CachePath);
                return;
            }
        }
    }
}
