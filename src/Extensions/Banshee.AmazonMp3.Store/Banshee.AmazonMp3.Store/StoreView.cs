//
// StoreView.cs
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

using Gtk;

using Hyena;
using Hyena.Downloader;

using Banshee.Base;
using Banshee.IO;
using Banshee.WebBrowser;
using Banshee.AmazonMp3;

namespace Banshee.AmazonMp3.Store
{
    public class StoreView : OssiferWebView
    {
        public StoreView ()
        {
            SetCookie ("dmusic_download_manager_enabled",
                AmzMp3Downloader.AmazonMp3DownloaderCompatVersion,
                ".amazon.com", "/", TimeSpan.FromDays (365.2422));

            LoadUri ("http://www.amazon.com/mp3/");
        }

        protected override OssiferNavigationResponse OnMimeTypePolicyDecisionRequested (string mimetype)
        {
            // We only explicitly accept (render) text/html types, and only
            // download audio/x-amzxml - everything else is ignored.
            switch (mimetype) {
                case "text/html": return OssiferNavigationResponse.Accept;
                case "audio/x-mpegurl":
                case "audio/x-amzxml": return OssiferNavigationResponse.Download;
                default:
                    Log.Debug ("OssiferWebView: ignoring mime type", mimetype);
                    return OssiferNavigationResponse.Ignore;
            }
        }

        protected override void OnLoadStatusChanged (OssiferLoadStatus status)
        {
            Console.WriteLine ("LOAD STATUS: {0} -> {1} ({2}, {3})", status, LoadStatus, Uri, Title);
        }

        protected override string OnDownloadRequested (string mimetype, string uri, string suggestedFilename)
        {
            switch (mimetype) {
                case "audio/x-amzurl":
                    var dest_uri_base = "file://" + Paths.Combine (Paths.TempDir, suggestedFilename);
                    var dest_uri = new SafeUri (dest_uri_base);
                    for (int i = 1; File.Exists (dest_uri);
                        dest_uri = new SafeUri (String.Format ("{0} ({1})", dest_uri_base, ++i)));
                    return dest_uri.AbsoluteUri;
                case "audio/x-mpegurl":
                    Banshee.Streaming.RadioTrackInfo.OpenPlay (uri);
                    return null;
            }

            return null;
        }

        protected override void OnDownloadStatusChanged (OssiferDownloadStatus status, string mimetype, string destinationUri)
        {
            // FIXME: handle the error case
            if (status != OssiferDownloadStatus.Finished) {
                return;
            }

            switch (mimetype) {
                case "audio/x-amzurl":
                    Log.Debug ("OssiferWebView: downloaded purchase list", destinationUri);
                    Banshee.ServiceStack.ServiceManager.Get<AmazonMp3DownloaderService> ()
                        .DownloadAmz (new SafeUri (destinationUri).LocalPath);
                    break;
            }
        }
    }
}