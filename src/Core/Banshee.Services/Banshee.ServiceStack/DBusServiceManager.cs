//
// DBusServiceManager.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;

using NDesk.DBus;
using org.freedesktop.DBus;

using Hyena;
using Banshee.Base;

namespace Banshee.ServiceStack
{
    public class DBusExportableAttribute : Attribute
    {
        private string service_name;
        public string ServiceName {
            get { return service_name; }
            set { service_name = value; }
        }
    }

    public class DBusServiceManager : IService, IRemoteServiceManager
    {
        public const string ObjectRoot = "/org/bansheeproject/Banshee";
        private Dictionary<object, ObjectPath> registered_objects = new Dictionary<object, ObjectPath> ();

        public static string MakeDBusSafeString (string str)
        {
            return str == null ? String.Empty : Regex.Replace (str, @"[^A-Za-z0-9]*", String.Empty);
        }

        public static string MakeObjectPath (IRemoteExportable o)
        {
            StringBuilder object_path = new StringBuilder ();

            object_path.Append (ObjectRoot);
            object_path.Append ('/');

            Stack<string> paths = new Stack<string> ();

            IRemoteExportable p = o.Parent;

            while (p != null) {
                paths.Push (String.Format ("{0}/", GetObjectName (p)));
                p = p.Parent;
            }

            while (paths.Count > 0) {
                object_path.Append (paths.Pop ());
            }

            object_path.Append (GetObjectName (o));

            return object_path.ToString ();
        }

        private static string GetObjectName (IRemoteExportable o)
        {
            return o is IRemoteObjectName ? ((IRemoteObjectName)o).ExportObjectName : o.ServiceName;
        }

        public static string [] MakeObjectPathArray<T> (IEnumerable<T> collection) where T : IRemoteExportable
        {
            List<string> paths = new List<string> ();

            foreach (IRemoteExportable item in collection) {
                paths.Add (MakeObjectPath (item));
            }

            return paths.ToArray ();
        }

        public bool Enabled {
            get { return DBusConnection.Enabled; }
        }

        public bool ServiceNameHasOwner (string serviceName)
        {
            return DBusConnection.NameHasOwner (serviceName);
        }

        public void Disconnect (string serviceName)
        {
            DBusConnection.Disconnect (serviceName);
        }

        public string RegisterObject (IRemoteExportable o)
        {
            return RegisterObject (DBusConnection.DefaultServiceName, o);
        }

        public string RegisterObject (string serviceName, IRemoteExportable o)
        {
            return RegisterObject (serviceName, o, MakeObjectPath (o));
        }

        public string RegisterObject (object o, string objectName)
        {
            return RegisterObject (DBusConnection.DefaultServiceName, o, objectName);
        }

        public string RegisterObject (string serviceName, object o, string objectName)
        {
            ObjectPath path = null;

            if (!DBusConnection.ConnectTried) {
                DBusConnection.Connect ();
            }

            if (DBusConnection.Enabled && Bus.Session != null) {
                IRemoteExportableProvider exportable_provider = o as IRemoteExportableProvider;
                if (exportable_provider != null) {
                    foreach (IRemoteExportable e in exportable_provider.Exportables) {
                        RegisterObject (e);
                    }
                }

                object [] attrs = o.GetType ().GetCustomAttributes (typeof (DBusExportableAttribute), true);
                if (attrs != null && attrs.Length > 0) {
                    DBusExportableAttribute dbus_attr = (DBusExportableAttribute)attrs[0];
                    if (!String.IsNullOrEmpty (dbus_attr.ServiceName)) {
                        serviceName = dbus_attr.ServiceName;
                    }
                }

                lock (registered_objects) {
                    registered_objects.Add (o, path = new ObjectPath (objectName));
                }

                string bus_name = DBusConnection.MakeBusName (serviceName);

                Log.DebugFormat ("Registering remote object {0} ({1}) on {2}", path, o.GetType (), bus_name);

                #pragma warning disable 0618
                Bus.Session.Register (bus_name, path, o);
                #pragma warning restore 0618

                Log.DebugFormat ("Registered remote object {0} ({1}) on {2}", path, o.GetType (), bus_name);
            }

            return path != null ? path.ToString () : null;
        }

        public void UnregisterObject (object o)
        {
            ObjectPath path = null;
            lock (registered_objects) {
                if (!registered_objects.TryGetValue (o, out path)) {
                    Log.WarningFormat ("Unable to unregister DBus object {0}, does not appear to be registered",
                        o.GetType ());
                    return;
                }

                registered_objects.Remove (o);
            }

            Bus.Session.Unregister (path);
        }

        public T FindInstance<T> (string objectPath) where T : class
        {
            return FindInstance<T> (DBusConnection.DefaultBusName, true, objectPath);
        }

        public T FindInstance<T> (string serviceName, string objectPath) where T : class
        {
            return FindInstance<T> (serviceName, false, objectPath);
        }

        public static T FindInstance<T> (string serviceName, bool isFullBusName, string objectPath) where T : class
        {
            string busName = isFullBusName ? serviceName : DBusConnection.MakeBusName (serviceName);
            if (!DBusConnection.Enabled || !Bus.Session.NameHasOwner (busName)) {
                return null;
            }

            string full_object_path = objectPath;
            if (!objectPath.StartsWith (ObjectRoot)) {
                full_object_path = ObjectRoot + objectPath;
            }

            return Bus.Session.GetObject<T> (busName, new ObjectPath (full_object_path));
        }

        string IService.ServiceName {
            get { return "DBusServiceManager"; }
        }
    }
}
