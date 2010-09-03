// 
// IpcRemotingApplicationInstance.cs
// 
// Author:
//   Pete Johanson <peter@peterjohanson.com>
// 
// Copyright 2010 Pete Johanson
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

using Hyena;

namespace Banshee.ServiceStack
{
    internal class IpcRemotingApplicationInstance : IApplicationInstance
    {
        #region IApplicationInstance Members

        public bool AlreadyRunning {
            get {
                ApplicationInstance inst = null;
                try {
                    inst = RemoteServiceManager.FindInstance<ApplicationInstance> ("/ApplicationInstance");
                    return inst != null && inst.Ping ();
                } catch (RemotingException) {
                    return false;
                } finally {
                    if (inst != null) {
                        try {
                            ((IDisposable)inst).Dispose ();
                        } catch { }
                    }
                }
            }
        }

        private bool connect_tried;
        public bool ConnectTried {
            get { return connect_tried; }
        }

        public void Connect ()
        {
            if (connect_tried)
                return;

            connect_tried = true;
        }

        public void Create ()
        {
            if (instance != null)
                return;

            instance = new ApplicationInstance ();

            RemoteServiceManager.RegisterObject (instance, "ApplicationInstance");
        }

        private GLib.MainLoop mainloop;
        public void RunMainLoop ()
        {
            Log.Debug ("IpcRemotingApplicationInstance.RunMainLoop");
            if (mainloop == null) {
                mainloop = new GLib.MainLoop ();
            }

            if (!mainloop.IsRunning) {
                mainloop.Run ();
            }
        }

        public void QuitMainLoop ()
        {
            Log.Debug ("IpcRemotingApplicationInstance.QuitMainLoop");
            if (mainloop != null && mainloop.IsRunning) {
                mainloop.Quit ();
            }
        }

        #endregion

        ApplicationInstance instance;

        internal class ApplicationInstance : MarshalByRefObject, IDisposable
        {
            public bool Ping ()
            {
                return true;
            }

            public override object InitializeLifetimeService ()
            {
                return null;
            }

            #region IDisposable Members

            public void Dispose ()
            {
            }

            #endregion
        }
    }
}
