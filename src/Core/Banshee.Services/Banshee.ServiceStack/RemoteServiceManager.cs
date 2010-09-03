// 
// RemoteServiceManager.cs
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

using Hyena;

namespace Banshee.ServiceStack
{
    public static class RemoteServiceManager
    {
        public static bool Enabled {
            get { return manager.Enabled; }
        }

        public static bool ServiceNameHasOwner (string serviceName)
        {
            return manager.ServiceNameHasOwner (serviceName);
        }

        public static T FindInstance<T> (string objectPath) where T : class
        {
            return manager.FindInstance<T> (objectPath);
        }

        public static T FindInstance<T> (string serviceName, string objectPath) where T : class
        {
            return manager.FindInstance<T> (serviceName, objectPath);
        }

        public static string RegisterObject (object o, string objectName)
        {
            return manager.RegisterObject (o, objectName);
        }

        public static string RegisterObject (IRemoteExportable o)
        {
            return manager.RegisterObject (o);
        }

        public static IEnumerable<string> RegisterObject (IRemoteExportableProvider exportable_provider)
        {
            // The ToList at the end ensures this enumerable is fully evaluationed and objects are registered.
            return exportable_provider.Exportables.Select (e => RegisterObject (e)).ToList ();
        }

        public static void UnregisterObject (object o)
        {
            manager.UnregisterObject (o);
        }

        public static void Disconnect (string serviceName)
        {
            manager.Disconnect (serviceName);
        }

        static RemoteServiceManager ()
        {
            if (Application.IsMSDotNet || ApplicationContext.CommandLine.Contains ("disable-dbus")) {
                manager = new IpcRemoteServiceManager ();
            } else {
                manager = new DBusServiceManager ();
            }
        }

        static IRemoteServiceManager manager;
    }
}
