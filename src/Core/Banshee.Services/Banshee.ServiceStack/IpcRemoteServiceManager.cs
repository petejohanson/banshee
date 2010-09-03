// 
// IpcRemoteServiceManager.cs
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
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Linq;
using System.Text;

using Hyena;
using Banshee.Base;

namespace Banshee.ServiceStack
{
    internal class IpcRemoteServiceManager : IRemoteServiceManager
    {
        #region IRemoteServiceManager Members

        public bool Enabled { get { return true; } }

        public bool ServiceNameHasOwner (string serviceName)
        {
            ServiceNameOwner owner = null;
            try {
                owner = FindInstance<ServiceNameOwner> (serviceName, "/ServiceNameOwner");

                return owner != null && owner.Ping ();
            } catch (RemotingException) {
                return false;
            } finally {
                if (owner != null) {
                    try {
                        ((IDisposable)owner).Dispose ();
                    } catch { }
                }
            }
        }

        public T FindInstance<T> (string objectPath) where T : class
        {
            return FindInstance<T> (DEFAULT_SERVICE_NAME, objectPath);
        }

        public T FindInstance<T> (string serviceName, string objectPath) where T : class
        {
            string url = CreateRemotingUrl (serviceName, objectPath);
            Log.DebugFormat ("Trying to find '{0}' at '{1}'", typeof (T), url);
            return (T)RemotingServices.Connect (typeof (T), url);
        }

        public string RegisterObject (object o, string objectName)
        {
            return RegisterObject (o, GetServiceName (o), objectName);
        }

        string RegisterObject (object o, string serviceName, string objectName)
        {
            IRemoteExportableProvider exportable_provider = o as IRemoteExportableProvider;
            if (exportable_provider != null) {
                foreach (IRemoteExportable e in exportable_provider.Exportables)
                    RegisterObject (e);
            }

            MarshalByRefObject ref_obj = o as MarshalByRefObject;

            if (ref_obj == null) {
                Log.WarningFormat ("Unable to register '{0}' as a remote object. Object is not a subclass of MarshalByRefObject.", objectName);
                return null;
            }

            EnsureServerChannel ();

            IncrementServiceNameUsage (serviceName);
            string url = CreateRemotingUrl (serviceName, objectName);

            foreach (Type t in o.GetType ().GetInterfaces ().Where (t => t.GetCustomAttributes (typeof (NDesk.DBus.InterfaceAttribute), false).Any ())) {
                // XXX: Proper/safe object name?
                RemoteObject (objectName, ref_obj, CreateObjectUrl (serviceName, objectName), t);
            }

            RemoteObject (objectName, ref_obj, CreateObjectUrl (serviceName, objectName), o.GetType ());

            return url;
        }

        private string GetServiceName (object o)
        {
            DBusExportableAttribute exportable_attr
                = o.GetType ().GetCustomAttributes (typeof (DBusExportableAttribute), true).OfType<DBusExportableAttribute> ().FirstOrDefault ();

            return exportable_attr != null ? exportable_attr.ServiceName : DEFAULT_SERVICE_NAME;
        }

        private void RemoteObject (string objectName, MarshalByRefObject ref_obj, string url, Type t)
        {
            ObjRef @ref = RemotingServices.Marshal (ref_obj, url, t);

            Log.DebugFormat ("Registered '{0}' of type '{1}' at '{2}'.", objectName, t, url);

            lock (this) {
                List<ObjRef> list = null;
                if (!registered_objects.TryGetValue (ref_obj, out list)) {
                    list = new List<ObjRef> ();
                    registered_objects[ref_obj] = list;
                }

                list.Add (@ref);
            }
        }

        public void UnregisterObject (object o)
        {
            MarshalByRefObject ref_obj = o as MarshalByRefObject;

            if (ref_obj == null) {
                // XXX: Warn here!
                return;
            }

            lock (this) {
                List<ObjRef> list;

                if (!registered_objects.TryGetValue (ref_obj, out list)) {
                    Log.WarningFormat ("Unable to unregister remote object {0}, does not appear to be registered",
                        o.GetType ());
                    return;
                }

                registered_objects.Remove (ref_obj);

                foreach (ObjRef @ref in list) {
                    RemotingServices.Unmarshal (@ref);
                }
            }

            DecrementServiceNameUsage (GetServiceName (o));
        }

        public string RegisterObject (IRemoteExportable o)
        {
            return RegisterObject (o, GetExportableServiceName (o));
        }

        private string GetExportableServiceName (IRemoteExportable o)
        {
            IRemoteObjectName obj_name = o as IRemoteObjectName;

            return obj_name != null ? obj_name.ExportObjectName : o.ServiceName;
        }

        public void Disconnect (string serviceName)
        {
            foreach (MarshalByRefObject obj in registered_objects.Keys.Where (o => GetServiceName (o) == serviceName && !(o is ServiceNameOwner)).ToList ()) {
                UnregisterObject (obj);
            }
        }

        #endregion

        public IpcRemoteServiceManager ()
        {
            ChannelServices.RegisterChannel (new IpcClientChannel (), false);
        }

        const string DEFAULT_NAMED_PIPE = "Banshee";
        const string DEFAULT_SERVICE_NAME = "Banshee";
        IChannel server_channel = null;
        Dictionary<MarshalByRefObject, List<ObjRef>> registered_objects = new Dictionary<MarshalByRefObject, List<ObjRef>> ();

        Dictionary<string, int> service_object_counts = new Dictionary<string, int> ();
        Dictionary<string, ServiceNameOwner> service_name_owners = new Dictionary<string, ServiceNameOwner> ();

        private void EnsureServerChannel ()
        {
            lock (this) {
                if (server_channel != null)
                    return;

                server_channel = new IpcServerChannel (DEFAULT_NAMED_PIPE, DEFAULT_NAMED_PIPE);
                ChannelServices.RegisterChannel (server_channel, false);
            }
        }

        private void IncrementServiceNameUsage (string serviceName)
        {
            lock (this) {
                if (!service_object_counts.ContainsKey (serviceName))
                    service_object_counts[serviceName] =  0;

                int count = service_object_counts[serviceName];

                service_object_counts[serviceName] = count + 1;

                if (count == 0) {
                    ServiceNameOwner owner = new ServiceNameOwner ();
                    service_name_owners[serviceName] = owner;
                    RegisterObject (owner, serviceName, "ServiceNameOwner");
                }
            }
        }

        private void DecrementServiceNameUsage (string serviceName)
        {
            lock (this) {
                if (!service_object_counts.ContainsKey (serviceName))
                    return;

                int new_count = service_object_counts[serviceName] - 1;

                service_object_counts[serviceName] = new_count;

                // We check for a count of *one*, since one of the
                // counts is from the ServiceNameOwner object.
                if (new_count == 1) {
                    ServiceNameOwner owner = service_name_owners[serviceName];
                    UnregisterObject (owner);
                    service_name_owners.Remove (serviceName);
                }
            }
        }

        private static string CreateObjectUrl (string serviceName, string objectPath)
        {
            return serviceName.Trim ('/') + "/" + objectPath.Trim ('/');
        }

        private static string CreateRemotingUrl (string serviceName, string objectPath)
        {
            return "ipc://" + DEFAULT_NAMED_PIPE + "/" + CreateObjectUrl (serviceName, objectPath);
        }

        class ServiceNameOwner : MarshalByRefObject, IDisposable
        {
            public bool Ping ()
            {
                Log.Debug ("ServiceNameOwner.Ping");
                return true;
            }

            #region IDisposable Members

            public void Dispose ()
            {
            }

            #endregion
        }

    }
}
