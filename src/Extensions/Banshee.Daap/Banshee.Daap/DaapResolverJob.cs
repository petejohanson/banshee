//
// DaapResolverJob.cs
//
// Authors:
//   Félix Velasco <felix.velasco@gmail.com>
//
// Copyright (C) 2009 Félix Velasco
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
using System.Net;

using Daap;
using NativeDaap = Daap;
using Hyena;
using Hyena.Jobs;

using Banshee.I18n;
using Banshee.ServiceStack;

namespace Banshee.Daap
{
    public class DaapResolverJob : SimpleAsyncJob
    {
        private IPAddress address;
        private ushort port;
        private Service service = null;
        private string server;

        public DaapResolverJob (string server, IPAddress address, ushort port) :
            base (String.Format (Catalog.GetString ("Connecting to {0}"), server), PriorityHints.None, null)
        {
            this.server = server;
            this.address = address;
            this.port = port;

            IsBackground = false;
            CanCancel = false;
            DelayShow = true;
            IconNames = new string [] { "applications-internet" };
        }

        protected override void Run ()
        {
            try {
                NativeDaap.Client tmp_client = new NativeDaap.Client (address, port);
                service = new Service (address, port, tmp_client.Name, tmp_client.AuthenticationMethod != AuthenticationMethod.None);
            } catch (WebException) {
                // On error, create a dummy service
                service = new Service (address, port, server, false);
            }

            OnFinished ();
        }

        public Service DaapService {
            get { return service; }
        }
    }
}
