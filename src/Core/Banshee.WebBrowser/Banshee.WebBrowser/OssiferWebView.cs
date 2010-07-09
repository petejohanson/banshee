// 
// OssiferWebView.cs
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
using System.Runtime.InteropServices;

namespace Banshee.WebBrowser
{
    public class OssiferWebView : Gtk.Widget
    {
        private delegate OssiferNavigationResponse MimeTypePolicyDecisionRequestedCallback (IntPtr ossifer, IntPtr mimetype);
        private delegate IntPtr DownloadRequestedCallback (IntPtr ossifer, IntPtr uri, IntPtr suggested_filename);
        private delegate void DocumentLoadFinishedCallback (IntPtr ossifer, IntPtr uri);
        private delegate void DownloadStatusChangedCallback (IntPtr ossifer, OssiferDownloadStatus status, IntPtr destnation_uri);

        [StructLayout (LayoutKind.Sequential)]
        private struct Callbacks
        {
            public MimeTypePolicyDecisionRequestedCallback MimeTypePolicyDecisionRequested;
            public DownloadRequestedCallback DownloadRequested;
            public DocumentLoadFinishedCallback DocumentLoadFinished;
            public DownloadStatusChangedCallback DownloadStatusChanged;
        }

        private const string LIBOSSIFER = "ossifer";

        private Callbacks callbacks;

        [DllImport (LIBOSSIFER)]
        private static extern IntPtr ossifer_web_view_get_type ();

        public static new GLib.GType GType {
            get { return new GLib.GType (ossifer_web_view_get_type ()); }
        }

        protected OssiferWebView (IntPtr raw) : base (raw)
        {
        }

        [DllImport (LIBOSSIFER)]
        private static extern void ossifer_web_view_set_callbacks (IntPtr ossifer, Callbacks callbacks);

        public OssiferWebView ()
        {
            Initialize ();
            CreateNativeObject (new string[0], new GLib.Value[0]);

            ossifer_web_view_set_callbacks (Handle, callbacks = new Callbacks () {
                MimeTypePolicyDecisionRequested =
                    new MimeTypePolicyDecisionRequestedCallback (HandleMimeTypePolicyDecisionRequested),
                DownloadRequested = new DownloadRequestedCallback (HandleDownloadRequested),
                DocumentLoadFinished = new DocumentLoadFinishedCallback (HandleDocumentLoadFinished),
                DownloadStatusChanged = new DownloadStatusChangedCallback (HandleDownloadStatusChanged)
            });

            GC.KeepAlive (callbacks);
        }

#region Callback Implementations

        private OssiferNavigationResponse HandleMimeTypePolicyDecisionRequested (IntPtr ossifer, IntPtr mimetype)
        {
            return OnMimeTypePolicyDecisionRequested (GLib.Marshaller.Utf8PtrToString (mimetype));
        }

        protected virtual OssiferNavigationResponse OnMimeTypePolicyDecisionRequested (string mimetype)
        {
            return OssiferNavigationResponse.Unhandled;
        }

        private IntPtr HandleDownloadRequested (IntPtr ossifer, IntPtr uri, IntPtr suggested_filename)
        {
            var destination_uri = OnDownloadRequested (
                GLib.Marshaller.Utf8PtrToString (uri),
                GLib.Marshaller.Utf8PtrToString (suggested_filename));
            return destination_uri == null
                ? IntPtr.Zero
                : GLib.Marshaller.StringToPtrGStrdup (destination_uri);
        }

        protected virtual string OnDownloadRequested (string uri, string suggestedFilename)
        {
            return null;
        }

        private void HandleDocumentLoadFinished (IntPtr ossifer, IntPtr uri)
        {
            OnDocumentLoadFinished (GLib.Marshaller.Utf8PtrToString (uri));
        }

        protected virtual void OnDocumentLoadFinished (string uri)
        {
        }

        private void HandleDownloadStatusChanged (IntPtr ossifer, OssiferDownloadStatus status, IntPtr destinationUri)
        {
            OnDownloadStatusChanged (status, GLib.Marshaller.Utf8PtrToString (destinationUri));
        }

        protected virtual void OnDownloadStatusChanged (OssiferDownloadStatus status, string destinationUri)
        {
        }

#endregion

#region Public Instance API

        [DllImport (LIBOSSIFER)]
        private static extern IntPtr ossifer_web_view_load_uri (IntPtr ossifer, IntPtr uri);

        public void LoadUri (string uri)
        {
            var uri_raw = IntPtr.Zero;
            try {
                uri_raw = GLib.Marshaller.StringToPtrGStrdup (uri);
                ossifer_web_view_load_uri (Handle, uri_raw);
            } finally {
                GLib.Marshaller.Free (uri_raw);
            }
        }

        [DllImport (LIBOSSIFER)]
        private static extern void ossifer_web_view_load_string (IntPtr ossifer,
            IntPtr content, IntPtr mimetype, IntPtr encoding, IntPtr base_uri);

        public void LoadString (string content, string mimetype, string encoding, string baseUri)
        {
            var content_raw = IntPtr.Zero;
            var mimetype_raw = IntPtr.Zero;
            var encoding_raw = IntPtr.Zero;
            var base_uri_raw = IntPtr.Zero;

            try {
                ossifer_web_view_load_string (Handle,
                    content_raw = GLib.Marshaller.StringToPtrGStrdup (content),
                    mimetype_raw = GLib.Marshaller.StringToPtrGStrdup (mimetype),
                    encoding_raw = GLib.Marshaller.StringToPtrGStrdup (encoding),
                    base_uri_raw = GLib.Marshaller.StringToPtrGStrdup (baseUri));
            } finally {
                GLib.Marshaller.Free (content_raw);
                GLib.Marshaller.Free (mimetype_raw);
                GLib.Marshaller.Free (encoding_raw);
                GLib.Marshaller.Free (base_uri_raw);
            }
        }

#endregion

#region Static API

        [DllImport (LIBOSSIFER)]
        private static extern void ossifer_web_view_global_initialize (IntPtr cookie_db_path);

        public static void Initialize ()
        {
            var path = System.IO.Path.Combine (Hyena.Paths.ApplicationData, "ossifer-browser-cookies");
            var path_raw = IntPtr.Zero;
            try {
                ossifer_web_view_global_initialize (path_raw = GLib.Marshaller.StringToPtrGStrdup (path));
            } finally {
                GLib.Marshaller.Free (path_raw);
            }
        }

        [DllImport (LIBOSSIFER)]
        private static extern void ossifer_web_view_global_set_cookie (IntPtr name, IntPtr value,
            IntPtr domain, IntPtr path, int max_age);

        public static void SetCookie (string name, string value, string domain, string path, TimeSpan maxAge)
        {
            var name_raw = IntPtr.Zero;
            var value_raw = IntPtr.Zero;
            var domain_raw = IntPtr.Zero;
            var path_raw = IntPtr.Zero;

            try {
                ossifer_web_view_global_set_cookie (
                    name_raw = GLib.Marshaller.StringToPtrGStrdup (name),
                    value_raw = GLib.Marshaller.StringToPtrGStrdup (value),
                    domain_raw = GLib.Marshaller.StringToPtrGStrdup (domain),
                    path_raw = GLib.Marshaller.StringToPtrGStrdup (path),
                    (int)Math.Round (maxAge.TotalSeconds));
            } finally {
                GLib.Marshaller.Free (name_raw);
                GLib.Marshaller.Free (value_raw);
                GLib.Marshaller.Free (domain_raw);
                GLib.Marshaller.Free (path_raw);
            }
        }

#endregion

    }
}