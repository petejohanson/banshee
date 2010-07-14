//
// MiroGuideView.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
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
using Banshee.ServiceStack;
using Banshee.WebBrowser;

namespace Banshee.MiroGuide
{
    public class MiroGuideView : Banshee.WebSource.WebView
    {
        public MiroGuideView ()
        {
            CanSearch = true;
            FullReload ();
        }

        protected override void OnLoadStatusChanged (OssiferLoadStatus status)
        {
            if ((status == OssiferLoadStatus.FirstVisuallyNonEmptyLayout ||
                status == OssiferLoadStatus.Finished) && Uri != "about:blank") {

                //if (fixup_javascript != null) {
                    //ExecuteScript (fixup_javascript);
                //}
                var fixup_javascript = @"
                    // Hide welcome
                    var w = document.getElementById ('welcome');
                    if (w != null) { w.style.display = 'none'; }

                    // Hide these
                    var a = document.getElementsByClassName ('only-in-browser')
                    for (var i = 0; i < a.length; i++) { a[i].style.display = 'none'; }

                    // Show these
                    a = document.getElementsByClassName ('only-in-miro')
                    for (var i = 0; i < a.length; i++) { a[i].className = ''; }
                ";
                ExecuteScript (fixup_javascript);
            }

            if (status == OssiferLoadStatus.Finished && Uri != null && Uri.StartsWith ("http://miroguide.com")) {
                last_was_audio.Set (Uri.Contains ("miroguide.com/audio/"));
            }

            base.OnLoadStatusChanged (status);
        }

        protected override OssiferNavigationResponse OnMimeTypePolicyDecisionRequested (string mimetype)
        {
            // We only explicitly accept (render) text/html types, and only
            // download audio/x-amzxml - everything else is ignored.
            switch (mimetype) {
                //case "audio/x-mpegurl":
                //
                // TODO:
                //   media mimetypes - ship to gst/PlayerEngine
                //   application/rss+xml
                case "application/x-miro":
                    return OssiferNavigationResponse.Download;
                default:
                    return base.OnMimeTypePolicyDecisionRequested (mimetype);
            }
        }

        protected override string OnDownloadRequested (string mimetype, string uri, string suggestedFilename)
        {
            switch (mimetype) {
                case "application/x-miro":
                    var dest_uri_base = "file://" + Paths.Combine (Paths.TempDir, suggestedFilename);
                    var dest_uri = new SafeUri (dest_uri_base);
                    for (int i = 1; File.Exists (dest_uri);
                        dest_uri = new SafeUri (String.Format ("{0} ({1})", dest_uri_base, ++i)));
                    return dest_uri.AbsoluteUri;
                /*case "audio/x-mpegurl":
                    Banshee.Streaming.RadioTrackInfo.OpenPlay (uri);
                    Banshee.ServiceStack.ServiceManager.PlaybackController.StopWhenFinished = true;
                    return null;*/
            }

            return null;
        }

        protected override OssiferNavigationResponse OnNavigationPolicyDecisionRequested (string uri)
        {
            Log.Information ("ResourceRequestStarting for", uri);

            // Avoid the whole redirect-before-downloading page if possible
            if (uri != null && uri.StartsWith ("http://subscribe.getmiro.com/") && uri.Contains ("url1")) {
                int a = uri.IndexOf ("url1") + 5;
                int l = Math.Min (uri.Length - 1, uri.IndexOf ('&', a)) - a;
                if (l > 0 && a + l < uri.Length) {
                    var direct_uri = System.Web.HttpUtility.UrlDecode (uri.Substring (a, l));
                    if (uri.Contains ("/download")) {
                        // Go straight to the destination URL
                        Banshee.Streaming.RadioTrackInfo.OpenPlay (direct_uri);
                        Banshee.ServiceStack.ServiceManager.PlaybackController.StopWhenFinished = true;
                        Log.DebugFormat ("MiroGuide: playing straight away {0}", direct_uri);
                    } else {
                        // Subscribe to it straight away, don't redirect at all
                        ServiceManager.Get<DBusCommandService> ().PushFile (direct_uri);
                        Log.DebugFormat ("MiroGuide: subscribing straight away to {0}", direct_uri);
                    }
                    return OssiferNavigationResponse.Ignore;
                }
            }

            return OssiferNavigationResponse.Unhandled;
        }

        protected override void OnDownloadStatusChanged (OssiferDownloadStatus status, string mimetype, string destinationUri)
        {
            // FIXME: handle the error case
            if (status != OssiferDownloadStatus.Finished) {
                return;
            }

            switch (mimetype) {
                case "application/x-miro":
                    Log.Debug ("MiroGuide: downloaded Miro subscription file", destinationUri);
                    ServiceManager.Get<DBusCommandService> ().PushFile (destinationUri);
                    break;
            }
        }

        public override void GoHome ()
        {
            if (last_was_audio.Get ()) {
                LoadUri ("http://miroguide.com/audio/#popular");
            } else {
                LoadUri ("http://miroguide.com/#popular");
            }
        }

        public override void GoSearch (string query)
        {
            if (last_was_audio.Get ()) {
                LoadUri (new Uri ("http://miroguide.com/audio/search?query=" + query).AbsoluteUri);
            } else {
                LoadUri (new Uri ("http://miroguide.com/search?query=" + query).AbsoluteUri);
            }
        }

        private static Banshee.Configuration.SchemaEntry<bool> last_was_audio = new Banshee.Configuration.SchemaEntry<bool> (
            "plugins.miroguide", "last_was_audio", true, "", ""
        );
    }
}
