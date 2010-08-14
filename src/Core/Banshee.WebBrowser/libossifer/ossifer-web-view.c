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

G_DEFINE_TYPE (OssiferWebView, ossifer_web_view, WEBKIT_TYPE_WEB_VIEW);

typedef WebKitNavigationResponse (* OssiferWebViewMimeTypePolicyDecisionRequestedCallback)
    (OssiferWebView *ossifer, const gchar *mimetype);

typedef WebKitNavigationResponse (* OssiferWebViewNavigationPolicyDecisionRequestedCallback)
    (OssiferWebView *ossifer, const gchar *uri);

typedef gchar * (* OssiferWebViewDownloadRequestedCallback)
    (OssiferWebView *ossifer, const gchar *mimetype, const gchar *uri, const gchar *suggested_filename);

typedef gchar * (* OssiferWebViewResourceRequestStartingCallback)
    (OssiferWebView *ossifer, const gchar *uri);

typedef void (* OssiferWebViewDownloadStatusChanged)
    (OssiferWebView *ossifer, WebKitDownloadStatus status, const gchar *mimetype, const gchar *uri);

typedef void (* OssiferWebViewLoadStatusChanged)
    (OssiferWebView *ossifer, WebKitLoadStatus status);

typedef struct {
    OssiferWebViewMimeTypePolicyDecisionRequestedCallback mime_type_policy_decision_requested;
    OssiferWebViewNavigationPolicyDecisionRequestedCallback navigation_policy_decision_requested;
    OssiferWebViewDownloadRequestedCallback download_requested;
    OssiferWebViewResourceRequestStartingCallback resource_request_starting;
    OssiferWebViewLoadStatusChanged load_status_changed;
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
ossifer_web_view_create_web_view (WebKitWebView *web_view, WebKitWebFrame *frame, gpointer user_data)
{
    return WEBKIT_WEB_VIEW (g_object_new (OSSIFER_TYPE_WEB_VIEW, NULL));
}

static void
ossifer_web_view_resource_request_starting (WebKitWebView *web_view, WebKitWebFrame *frame,
    WebKitWebResource *resource, WebKitNetworkRequest *request,
    WebKitNetworkResponse *response, gpointer user_data)
{
    OssiferWebView *ossifer = OSSIFER_WEB_VIEW (web_view);
    const gchar *old_uri;
    gchar *new_uri = NULL;

    if (ossifer->priv->callbacks.resource_request_starting != NULL) {
        old_uri = webkit_network_request_get_uri (request);
        new_uri = ossifer->priv->callbacks.resource_request_starting (ossifer, old_uri);
        if (new_uri) {
            webkit_network_request_set_uri (request, new_uri);
            g_free (new_uri);
        }
    }
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

static gboolean
ossifer_web_view_navigation_policy_decision_requested (WebKitWebView *web_view, WebKitWebFrame *frame,
    WebKitNetworkRequest *request, WebKitWebNavigationAction *action, WebKitWebPolicyDecision *policy_decision, gpointer user_data)
{
    OssiferWebView *ossifer = OSSIFER_WEB_VIEW (web_view);

    if (ossifer->priv->callbacks.navigation_policy_decision_requested == NULL) {
        return FALSE;
    }

    const gchar * uri = webkit_network_request_get_uri (request);
    switch ((gint)ossifer->priv->callbacks.navigation_policy_decision_requested (ossifer, uri)) {
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
ossifer_web_view_notify_load_status (GObject* object, GParamSpec* pspec, gpointer user_data)
{
    OssiferWebView *ossifer = OSSIFER_WEB_VIEW (object);

    if (ossifer->priv->callbacks.load_status_changed != NULL) {
        ossifer->priv->callbacks.load_status_changed (ossifer,
            webkit_web_view_get_load_status (WEBKIT_WEB_VIEW (ossifer)));
    }
}

static GtkWidget *
ossifer_web_view_create_plugin_widget (WebKitWebView *web_view, gchar *mime_type,
    gchar *uri, GHashTable *param, gpointer user_data)
{
    // FIXME: this is just a useless stub, but could be used to provide
    // overriding plugins that hook directly into Banshee - e.g. provide
    // in-page controls that match the functionality of Amazon's MP3
    // preview Flash control.
    //
    // I'm opting not to do this now, because this requires setting
    // "enable-plugins" to TRUE, which causes all the plugins to be
    // loaded, which can introduce instability. There should be a fix
    // to avoid building the plugin registry at all in libwebkit.
    return NULL;
}

// ---------------------------------------------------------------------------
// OssiferWebView Class/Object Implementation
// ---------------------------------------------------------------------------

static void
ossifer_web_view_class_init (OssiferWebViewClass *klass)
{
    g_type_class_add_private (klass, sizeof (OssiferWebViewPrivate));
}

static void
ossifer_web_view_init (OssiferWebView *ossifer)
{
    WebKitWebSettings *settings;
    
    ossifer->priv = G_TYPE_INSTANCE_GET_PRIVATE (ossifer, OSSIFER_TYPE_WEB_VIEW, OssiferWebViewPrivate);

    g_object_get (ossifer, "settings", &settings, NULL);
    g_object_set (settings,
        "enable-plugins", FALSE,
        "enable-page-cache", TRUE,
        "enable-default-context-menu", FALSE,
        NULL);

    g_object_set (ossifer,
        "full-content-zoom", TRUE,
        NULL);

    g_signal_connect (ossifer, "mime-type-policy-decision-requested",
        G_CALLBACK (ossifer_web_view_mime_type_policy_decision_requested), NULL);

    g_signal_connect (ossifer, "navigation-policy-decision-requested",
        G_CALLBACK (ossifer_web_view_navigation_policy_decision_requested), NULL);

    g_signal_connect (ossifer, "download-requested",
        G_CALLBACK (ossifer_web_view_download_requested), NULL);

    g_signal_connect (ossifer, "notify::load-status",
        G_CALLBACK (ossifer_web_view_notify_load_status), NULL);

    g_signal_connect (ossifer, "create-plugin-widget",
        G_CALLBACK (ossifer_web_view_create_plugin_widget), NULL);

    g_signal_connect (ossifer, "create-web-view",
        G_CALLBACK (ossifer_web_view_create_web_view), NULL);

    g_signal_connect (ossifer, "resource-request-starting",
        G_CALLBACK (ossifer_web_view_resource_request_starting), NULL);
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

const gchar *
ossifer_web_view_get_uri (OssiferWebView *ossifer)
{
    g_return_val_if_fail (OSSIFER_WEB_VIEW (ossifer), NULL);
    return webkit_web_view_get_uri (WEBKIT_WEB_VIEW (ossifer));
}

const gchar *
ossifer_web_view_get_title (OssiferWebView *ossifer)
{
    g_return_val_if_fail (OSSIFER_WEB_VIEW (ossifer), NULL);
    return webkit_web_view_get_title (WEBKIT_WEB_VIEW (ossifer));
}

WebKitLoadStatus
ossifer_web_view_get_load_status (OssiferWebView *ossifer)
{
    g_return_val_if_fail (OSSIFER_WEB_VIEW (ossifer), WEBKIT_LOAD_FAILED);
    return webkit_web_view_get_load_status (WEBKIT_WEB_VIEW (ossifer));
}

gboolean
ossifer_web_view_can_go_back (OssiferWebView *ossifer)
{
    g_return_val_if_fail (OSSIFER_WEB_VIEW (ossifer), FALSE);
    return webkit_web_view_can_go_back (WEBKIT_WEB_VIEW (ossifer));
}

gboolean
ossifer_web_view_can_go_forward (OssiferWebView *ossifer)
{
    g_return_val_if_fail (OSSIFER_WEB_VIEW (ossifer), FALSE);
    return webkit_web_view_can_go_forward (WEBKIT_WEB_VIEW (ossifer));
}

void
ossifer_web_view_go_back (OssiferWebView *ossifer)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    return webkit_web_view_go_back (WEBKIT_WEB_VIEW (ossifer));
}

void
ossifer_web_view_go_forward (OssiferWebView *ossifer)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    return webkit_web_view_go_forward (WEBKIT_WEB_VIEW (ossifer));
}

void
ossifer_web_view_reload (OssiferWebView *ossifer)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    return webkit_web_view_reload (WEBKIT_WEB_VIEW (ossifer));
}

void
ossifer_web_view_set_zoom (OssiferWebView *ossifer, gfloat zoomLevel)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    return webkit_web_view_set_zoom_level (WEBKIT_WEB_VIEW (ossifer), zoomLevel);
}

gfloat
ossifer_web_view_get_zoom (OssiferWebView *ossifer)
{
    g_return_val_if_fail (OSSIFER_WEB_VIEW (ossifer), 1);
    return webkit_web_view_get_zoom_level (WEBKIT_WEB_VIEW (ossifer));
}

void
ossifer_web_view_reload_bypass_cache (OssiferWebView *ossifer)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    return webkit_web_view_reload_bypass_cache (WEBKIT_WEB_VIEW (ossifer));
}

void
ossifer_web_view_execute_script (OssiferWebView *ossifer, const gchar *script)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    return webkit_web_view_execute_script (WEBKIT_WEB_VIEW (ossifer), script);
}
