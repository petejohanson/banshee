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
        private string fixup_javascript;
        private bool fixup_javascript_fetched;

        public bool IsSignedIn { get; private set; }
        public bool IsReady { get; private set; }

        public event EventHandler SignInChanged;
        public event EventHandler Ready;

        public StoreView ()
        {
            OssiferSession.CookieChanged += (o, n) => CheckSignIn ();

            // Ensure that Amazon knows a valid downloader is available,
            // otherwise the purchase experience is interrupted with a
            // confusing message about downloading and installing software.
            OssiferSession.SetCookie ("dmusic_download_manager_enabled",
                AmzMp3Downloader.AmazonMp3DownloaderCompatVersion,
                ".amazon.com", "/", TimeSpan.FromDays (365.2422));

            CheckSignIn ();
            FullReload ();
        }

        protected override void OnLoadStatusChanged (OssiferLoadStatus status)
        {
            if ((status == OssiferLoadStatus.FirstVisuallyNonEmptyLayout ||
                status == OssiferLoadStatus.Finished) && Uri != "about:blank") {
                // Hide "Install Flash" messages on Amazon since we
                // play content previews natively in Banshee
                if (fixup_javascript != null) {
                    ExecuteScript (fixup_javascript);
                }
            }

            base.OnLoadStatusChanged (status);
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

        protected override string OnDownloadRequested (string mimetype, string uri, string suggestedFilename)
        {
            switch (mimetype) {
                case "audio/x-amzxml":
                    var dest_uri_base = "file://" + Paths.Combine (Paths.TempDir, suggestedFilename);
                    var dest_uri = new SafeUri (dest_uri_base);
                    for (int i = 1; File.Exists (dest_uri);
                        dest_uri = new SafeUri (String.Format ("{0} ({1})", dest_uri_base, ++i)));
                    return dest_uri.AbsoluteUri;
                case "audio/x-mpegurl":
                    Banshee.Streaming.RadioTrackInfo.OpenPlay (uri);
                    Banshee.ServiceStack.ServiceManager.PlaybackController.StopWhenFinished = true;
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
                case "audio/x-amzxml":
                    Log.Debug ("OssiferWebView: downloaded purchase list", destinationUri);
                    Banshee.ServiceStack.ServiceManager.Get<AmazonMp3DownloaderService> ()
                        .DownloadAmz (new SafeUri (destinationUri).LocalPath);
                    break;
            }
        }

        public void GoHome ()
        {
            LoadUri ("http://amz-proxy.banshee.fm/do/home/");
        }

        public void GoSearch (string query)
        {
            LoadUri (new Uri ("http://amz-proxy.banshee.fm/do/search/" + query).AbsoluteUri);
        }

        public void SignOut ()
        {
            foreach (var name in new [] { "at-main", "x-main", "session-id",
                "session-id-time", "session-token", "uidb-main", "pf"}) {
                OssiferSession.DeleteCookie (name, ".amazon.com", "/");
            }

            FullReload ();
        }

        public void FullReload ()
        {
            // This is an HTML5 Canvas/JS spinner icon. It is awesome
            // and renders immediately, going away when the store loads.
            LoadString (AssemblyResource.GetFileContents ("loading.html"),
                "text/html", "UTF-8", null);

            // Here we download and save for later injection some JavaScript
            // to fix-up the Amazon pages. We don't store this locally since
            // it may need to be updated if Amazon's page structure changes.
            // We're mainly concerned about hiding the "You don't have Flash"
            // messages, since we do the streaming of previews natively.
            if (!fixup_javascript_fetched) {
                fixup_javascript_fetched = true;
                new Hyena.Downloader.HttpStringDownloader () {
                    Uri = new Uri ("http://amz-proxy.banshee.fm/amz-fixups.js"),
                    Finished = (d) => {
                        if (d.State.Success) {
                            fixup_javascript = d.Content;
                        }
                        LoadHome ();
                    },
                    AcceptContentTypes = new [] { "text/javascript" }
                }.Start ();
            } else {
                LoadHome ();
            }
        }

        private void LoadHome ()
        {
            // We defer this to another main loop iteration, otherwise
            // our load placeholder document will never be rendered.
            GLib.Idle.Add (delegate {
                GoHome ();

                // Emit the Ready event once we are first allowed
                // to load the home page (ensures we've downloaded
                // the fixup javascript, etc.).
                if (!IsReady) {
                    IsReady = true;
                    var handler = Ready;
                    if (handler != null) {
                        handler (this, EventArgs.Empty);
                    }
                }
                return false;
            });
        }

        private void CheckSignIn ()
        {
            var signed_in = OssiferSession.GetCookie ("at-main", ".amazon.com", "/") != null;
            if (IsSignedIn != signed_in) {
                IsSignedIn = signed_in;
                OnSignInChanged ();
            }
        }

        protected virtual void OnSignInChanged ()
        {
            var handler = SignInChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
    }
}
