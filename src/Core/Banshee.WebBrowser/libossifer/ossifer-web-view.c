//
// ossifer-web-view.c
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

#include <config.h>
#include "ossifer-web-view.h"

#ifdef HAVE_LIBSOUP_GNOME
#  include <libsoup/soup-gnome.h>
#endif

G_DEFINE_TYPE (OssiferWebView, ossifer_web_view, WEBKIT_TYPE_WEB_VIEW);

typedef WebKitNavigationResponse (* OssiferWebViewMimeTypePolicyDecisionRequestedCallback)
    (OssiferWebView *ossifer, const gchar *mimetype);

typedef gchar * (* OssiferWebViewDownloadRequestedCallback)
    (OssiferWebView *ossifer, const gchar *mimetype, const gchar *uri, const gchar *suggested_filename);

typedef void (* OssiferWebViewDocumentLoadFinishedCallback)
    (OssiferWebView *ossifer, const gchar *uri);

typedef void (* OssiferWebViewDownloadStatusChanged)
    (OssiferWebView *ossifer, WebKitDownloadStatus status, const gchar *mimetype, const gchar *uri);

typedef struct {
    OssiferWebViewMimeTypePolicyDecisionRequestedCallback mime_type_policy_decision_requested;
    OssiferWebViewDownloadRequestedCallback download_requested;
    OssiferWebViewDocumentLoadFinishedCallback document_load_finished;
    OssiferWebViewDownloadStatusChanged download_status_changed;
} OssiferWebViewCallbacks;

struct OssiferWebViewPrivate {
    OssiferWebViewCallbacks callbacks;
};

// ---------------------------------------------------------------------------
// OssiferWebView Internal Implementation
// ---------------------------------------------------------------------------

static const gchar *
ossifer_web_view_download_get_mimetype (WebKitDownload *download)
{
    return soup_message_headers_get_content_type (
        webkit_network_response_get_message (
            webkit_download_get_network_response (download)
        )->response_headers, NULL);
}

static WebKitWebView *
ossifer_web_view_create_web_view (WebKitWebView *web_view, WebKitWebFrame *web_frame)
{
    return WEBKIT_WEB_VIEW (g_object_new (OSSIFER_TYPE_WEB_VIEW, NULL));
}

static gboolean
ossifer_web_view_mime_type_policy_decision_requested (WebKitWebView *web_view, WebKitWebFrame *frame,
    WebKitNetworkRequest *request, gchar *mimetype, WebKitWebPolicyDecision *policy_decision, gpointer user_data)
{
    OssiferWebView *ossifer = OSSIFER_WEB_VIEW (web_view);

    if (ossifer->priv->callbacks.mime_type_policy_decision_requested == NULL) {
        return FALSE;
    }

    switch ((gint)ossifer->priv->callbacks.mime_type_policy_decision_requested (ossifer, mimetype)) {
        case 1000 /* Ossifer addition for 'unhandled' */:
            return FALSE;
        case (gint)WEBKIT_NAVIGATION_RESPONSE_DOWNLOAD:
            webkit_web_policy_decision_download (policy_decision);
            break;
        case (gint)WEBKIT_NAVIGATION_RESPONSE_IGNORE:
            webkit_web_policy_decision_ignore (policy_decision);
            break;
        case (gint)WEBKIT_NAVIGATION_RESPONSE_ACCEPT:
        default:
            webkit_web_policy_decision_use (policy_decision);
            break;
    }

    return TRUE;
}

static void
ossifer_web_view_download_notify_status (GObject* object, GParamSpec* pspec, gpointer user_data)
{
    OssiferWebView *ossifer = OSSIFER_WEB_VIEW (user_data);
    WebKitDownload* download = WEBKIT_DOWNLOAD (object);

    if (ossifer->priv->callbacks.download_status_changed != NULL) {
        ossifer->priv->callbacks.download_status_changed (ossifer,
            webkit_download_get_status (download),
            ossifer_web_view_download_get_mimetype (download),
            webkit_download_get_destination_uri (download));
    }
}

static gboolean
ossifer_web_view_download_requested (WebKitWebView *web_view, WebKitDownload *download, gpointer user_data)
{
    OssiferWebView *ossifer = OSSIFER_WEB_VIEW (web_view);
    gchar *destination_uri;

    if (ossifer->priv->callbacks.download_requested == NULL ||
        (destination_uri = ossifer->priv->callbacks.download_requested (
            ossifer,
            ossifer_web_view_download_get_mimetype (download),
            webkit_download_get_uri (download),
            webkit_download_get_suggested_filename (download))) == NULL) {
        return FALSE;
    }

    webkit_download_set_destination_uri (download, destination_uri);

    g_signal_connect (download, "notify::status",
        G_CALLBACK (ossifer_web_view_download_notify_status), ossifer);

    g_free (destination_uri);

    return TRUE;
}

static void
ossifer_web_view_document_load_finished (WebKitWebView *web_view, WebKitWebFrame *web_frame, gpointer user_data)
{
    OssiferWebView *ossifer = OSSIFER_WEB_VIEW (web_view);

    if (ossifer->priv->callbacks.document_load_finished != NULL) {
        ossifer->priv->callbacks.document_load_finished (ossifer, webkit_web_frame_get_uri (web_frame));
    }
}

// ---------------------------------------------------------------------------
// OssiferWebView Class/Object Implementation
// ---------------------------------------------------------------------------

static void
ossifer_web_view_finalize (GObject *object)
{
    G_OBJECT_CLASS (ossifer_web_view_parent_class)->finalize (object);
}

static void
ossifer_web_view_class_init (OssiferWebViewClass *klass)
{
    GObjectClass *object_class = G_OBJECT_CLASS (klass);
    WebKitWebViewClass *web_view_class = WEBKIT_WEB_VIEW_CLASS (klass);

    g_type_class_add_private (klass, sizeof (OssiferWebViewPrivate));

    object_class->finalize = ossifer_web_view_finalize;

    web_view_class->create_web_view = ossifer_web_view_create_web_view;
}

static void
ossifer_web_view_init (OssiferWebView *ossifer)
{
    WebKitWebSettings *settings;
    
    ossifer->priv = G_TYPE_INSTANCE_GET_PRIVATE (ossifer, OSSIFER_TYPE_WEB_VIEW, OssiferWebViewPrivate);

    g_object_get (ossifer, "settings", &settings, NULL);
    g_object_set (settings,
        "enable-plugins", FALSE,
        "enable-default-context-menu", FALSE,
        NULL);

    g_signal_connect (ossifer, "mime-type-policy-decision-requested",
        G_CALLBACK (ossifer_web_view_mime_type_policy_decision_requested), NULL);

    g_signal_connect (ossifer, "download-requested",
        G_CALLBACK (ossifer_web_view_download_requested), NULL);

    g_signal_connect (ossifer, "document-load-finished",
        G_CALLBACK (ossifer_web_view_document_load_finished), NULL);
}


// ---------------------------------------------------------------------------
// OssiferWebView Public Static API
// ---------------------------------------------------------------------------

void
ossifer_web_view_global_initialize (const gchar *cookie_db_path)
{
    static gboolean already_initialized = FALSE;

    SoupSession *session;
    SoupCookieJar *cookies;
    gchar *path;

    if (already_initialized) {
        return;
    }

    already_initialized = TRUE;

    session = webkit_get_default_session ();

#ifdef HAVE_LIBSOUP_GNOME
    path = g_strdup_printf ("%s.sqlite", cookie_db_path);
    cookies = soup_cookie_jar_sqlite_new (path, FALSE);
#else
    path = g_strdup_printf ("%s.txt", cookie_db_path);
    cookies = soup_cookie_jar_text_new (path, FALSE);
#endif
    soup_session_add_feature (session, SOUP_SESSION_FEATURE (cookies));
    g_object_unref (cookies);
    g_free (path);

#ifdef HAVE_LIBSOUP_GNOME
    soup_session_add_feature_by_type (session, SOUP_TYPE_PROXY_RESOLVER_GNOME);
#endif
}

void
ossifer_web_view_global_set_cookie (const gchar *name, const gchar *value,
    const gchar *domain, const gchar *path, gint max_age)
{
    SoupSession *session;
    SoupCookieJar *cookies;
    SoupCookie *cookie;

    session = webkit_get_default_session ();
    cookies = (SoupCookieJar *)soup_session_get_feature (session, SOUP_TYPE_COOKIE_JAR);

    g_return_if_fail (cookies != NULL);

    cookie = soup_cookie_new (name, value, domain, path, max_age);
    soup_cookie_jar_add_cookie (cookies, cookie);
}

// ---------------------------------------------------------------------------
// OssiferWebView Public Instance API
// ---------------------------------------------------------------------------

void
ossifer_web_view_set_callbacks (OssiferWebView *ossifer, OssiferWebViewCallbacks callbacks)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    ossifer->priv->callbacks = callbacks;
}

void
ossifer_web_view_load_uri (OssiferWebView *ossifer, const gchar *uri)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    webkit_web_view_load_uri (WEBKIT_WEB_VIEW (ossifer), uri);
}

void
ossifer_web_view_load_string (OssiferWebView *ossifer, const gchar *content,
    const gchar *mimetype, const gchar *encoding,  const gchar *base_uri)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    webkit_web_view_load_string (WEBKIT_WEB_VIEW (ossifer), content, mimetype, encoding, base_uri);
}