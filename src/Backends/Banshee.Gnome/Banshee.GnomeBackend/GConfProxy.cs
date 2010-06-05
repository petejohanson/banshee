// 
// GConfProxy.cs
// 
// Author:
//   Iain Lane <laney@ubuntu.com>
// 
// Copyright 2010 Iain Lane
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

using Banshee.Configuration;

namespace Banshee.GnomeBackend
{
    public class GConfProxy
    {
        const string PROXY = "/system/http_proxy";
        const string PROXY_USE_PROXY = "use_http_proxy";
        const string PROXY_USE_AUTH = "use_authentication";
        const string PROXY_HOST = "host";
        const string PROXY_PORT = "port";
        const string PROXY_USER = "authentication_user";
        const string PROXY_PASSWORD = "authentication_password";
        const string PROXY_BYPASS_LIST = "ignore_hosts";

        private bool use_proxy, use_auth;
        private string proxy_host, proxy_user, proxy_password;
        private int proxy_port;
        private string[] proxy_bypass_list;

        private GConfConfigurationClient gconf_configuration_client;

        public GConfProxy ()
        {
            gconf_configuration_client = new GConfConfigurationClient ();

            // Set them up the initial DefaultWebProxy
            System.Net.HttpWebRequest.DefaultWebProxy = GetProxyFromGConf ();

            // And hook up a callback so that we get notified of any changes in the proxy settings
            AddCallback ();
        }

        private void AddCallback ()
        {
            gconf_configuration_client.AddCallback (PROXY, new GConf.NotifyEventHandler(OnProxyChanged));
        }

        private void OnProxyChanged (object sender, GConf.NotifyEventArgs args)
        {
            Log.Debug ("Updating web proxy");
            HttpWebRequest.DefaultWebProxy = GetProxyFromGConf ();
        }

        private WebProxy GetProxyFromGConf ()
        {
            // Read the settings in from GConf
            use_proxy = gconf_configuration_client.Get<bool> (PROXY, PROXY_USE_PROXY, false);
            use_auth = gconf_configuration_client.Get<bool> (PROXY,  false);
            proxy_host = gconf_configuration_client.Get<string> (PROXY, PROXY_HOST, null);
            proxy_port = gconf_configuration_client.Get<int> (PROXY, PROXY_PORT, 0);
            proxy_user = gconf_configuration_client.Get<string> (PROXY,  null);
            proxy_password = gconf_configuration_client.Get<string> (PROXY, null);
            proxy_bypass_list = gconf_configuration_client.Get<string[]> (PROXY, PROXY_BYPASS_LIST, new string[0]);

            WebProxy proxy = new WebProxy ();

            // No proxy set, just return the empty proxy
            if (!use_proxy || proxy_host == null) {
                Log.Debug ("No proxy in use");
                return proxy;
            }

            // otherwise we have a proxy. Let's get this show on the road.

            // First we need to construct the uri of the proxy

            string uri = String.Format ("http://{0}:{1}", proxy_host, proxy_port);
            proxy.Address = new Uri (uri);

            // Next the list of websites to bypass the proxy for.
            foreach (string host in proxy_bypass_list) {
                if (host.Contains ("*.local")) {
                    proxy.BypassProxyOnLocal = true;
                    continue;
                }

                proxy.BypassArrayList.Add (string.Format ("http://{0}", host));
            }

            // and finally we might need to authenticate, so let's do that
            if (use_auth) {
                proxy.Credentials = new NetworkCredential (proxy_user, proxy_password);
            } else {
                proxy.Credentials = null;
            }

            Log.Debug (String.Format ("Set web proxy to: {0}", proxy.Address.AbsoluteUri));

            return proxy;
        }
    }
}