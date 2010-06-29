// 
// GConfProxy.cs
// 
// Author:
//   Iain Lane <laney@ubuntu.com>
//   Ting Z Zhou <ting.z.zhou@intel.com>
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2010 Iain Lane
// Copyright 2010 Intel Corp
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
using System.Net;

using Hyena;

namespace Banshee.GnomeBackend
{
    public class GConfProxy : IDisposable
    {
        private const string PROXY = "/system/proxy";
        private const string PROXY_MODE = "mode";
        private const string PROXY_AUTO_URL = "autoconfig_url";
        private const string HTTP_PROXY = "/system/http_proxy";
        private const string PROXY_USE_PROXY = "use_http_proxy";
        private const string PROXY_USE_AUTH = "use_authentication";
        private const string PROXY_HOST = "host";
        private const string PROXY_PORT = "port";
        private const string PROXY_USER = "authentication_user";
        private const string PROXY_PASSWORD = "authentication_password";
        private const string PROXY_BYPASS_LIST = "ignore_hosts";

        private GConf.Client gconf_client;
        private uint refresh_id;

        public GConfProxy ()
        {
            gconf_client = new GConf.Client ();
            gconf_client.AddNotify (PROXY, OnGConfNotify);
            gconf_client.AddNotify (HTTP_PROXY, OnGConfNotify);

            RefreshProxy ();
        }

        public void Dispose ()
        {
            if (gconf_client != null) {
                gconf_client.RemoveNotify (PROXY, OnGConfNotify);
                gconf_client.RemoveNotify (HTTP_PROXY, OnGConfNotify);
                gconf_client = null;
            }
        }

        private void OnGConfNotify (object o, GConf.NotifyEventArgs args)
        {
            if (refresh_id > 0) {
                return;
            }

            // Wait 5 seconds before reloading the proxy, and block any
            // other notifications. This notification will be raised on
            // any minor change (e.g. htt->http->http:->http:/->http://)
            // to any of the GNOME proxy settings. Also, at any given
            // point in the modification of the settings, the state may
            // be invalid, so retain the previous good configuration.
            refresh_id = GLib.Timeout.Add (5000, RefreshProxy);
        }

        private bool RefreshProxy ()
        {
            Log.Information ("Updating web proxy from GConf");
            try {
                HttpWebRequest.DefaultWebProxy = GetProxyFromGConf ();
            } catch {
                Log.Warning ("Not updating proxy settings. Invalid state");
            }

            refresh_id = 0;
            return false;
        }

        private T Get<T> (string @namespace, string key)
        {
            try {
                return (T)gconf_client.Get (@namespace == null
                    ? key
                    : @namespace + "/" + key);
            } catch {
                return default (T);
            }
        }

        private WebProxy GetProxyFromGConf ()
        {
            var proxy_mode = Get<string> (PROXY, PROXY_MODE);
            var proxy_auto_url = Get<string> (PROXY, PROXY_AUTO_URL);
            var use_proxy = Get<bool> (HTTP_PROXY, PROXY_USE_PROXY);
            var use_auth = Get<bool> (null, HTTP_PROXY);
            var proxy_host = Get<string> (HTTP_PROXY, PROXY_HOST);
            var proxy_port = Get<int> (HTTP_PROXY, PROXY_PORT);
            var proxy_user = Get<string> (null, HTTP_PROXY);
            var proxy_password = Get<string> (null, HTTP_PROXY);
            var proxy_bypass_list = Get<string[]> (HTTP_PROXY, PROXY_BYPASS_LIST);

            if (!use_proxy || proxy_mode == "none") {
                Log.Debug ("Direct connection, no proxy in use");
                return null;
            }

            var proxy = new WebProxy ();

            if (proxy_mode == "auto") {
                if (!String.IsNullOrEmpty (proxy_auto_url)) {
                    proxy.Address = new Uri (proxy_auto_url);
                    Log.Debug ("Automatic proxy connection", proxy.Address.AbsoluteUri);
                } else {
                    Log.Warning ("Direct connection, no proxy in use. Proxy mode was 'auto' but no automatic configuration URL was found.");
                    return null;
                }
            } else {
                proxy.Address = new Uri (String.Format ("http://{0}:{1}", proxy_host, proxy_port));
                proxy.Credentials = use_auth
                    ? new NetworkCredential (proxy_user, proxy_password)
                    : null;
                Log.Debug ("Manual proxy connection", proxy.Address.AbsoluteUri);
            }

            if (proxy_bypass_list == null) {
                return proxy;
            }

            foreach (var host in proxy_bypass_list) {
                if (host.Contains ("*.local")) {
                    proxy.BypassProxyOnLocal = true;
                    continue;
                }

                proxy.BypassArrayList.Add (String.Format ("http://{0}", host));
            }

            return proxy;
        }
    }
}
