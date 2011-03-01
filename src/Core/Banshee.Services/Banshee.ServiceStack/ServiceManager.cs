//
// ServiceManager.cs
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
using System.Linq;
using System.IO;
using System.Collections.Generic;

using Mono.Addins;

using Hyena;

using Banshee.Base;
using Banshee.MediaProfiles;
using Banshee.Sources;
using Banshee.Database;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.Library;
using Banshee.Hardware;

namespace Banshee.ServiceStack
{
    public static class ServiceManager
    {
        private static Dictionary<string, IService> services = new Dictionary<string, IService> ();
        private static Dictionary<string, IExtensionService> extension_services = new Dictionary<string, IExtensionService> ();
        private static Stack<IService> dispose_services = new Stack<IService> ();
        private static List<Type> service_types = new List<Type> ();
        private static ExtensionNodeList extension_nodes;

        private static bool is_initialized = false;
        private static readonly object self_mutex = new object ();

        public static event EventHandler StartupBegin;
        public static event EventHandler StartupFinished;
        public static event ServiceStartedHandler ServiceStarted;

        public static void Initialize ()
        {
            Application.ClientStarted += OnClientStarted;
        }

        public static void InitializeAddins ()
        {
            AddinManager.Initialize (ApplicationContext.CommandLine.Contains ("uninstalled")
                ? "." : Paths.ApplicationData);

            IProgressStatus monitor = ApplicationContext.CommandLine.Contains ("debug-addins")
                ? new ConsoleProgressStatus (true)
                : null;

            AddinManager.AddinLoadError += (o, a) => {
                try {
                    AddinManager.Registry.DisableAddin (a.AddinId);
                } catch {}
                Log.Exception (a.Message, a.Exception);
            };

            if (ApplicationContext.Debugging) {
                AddinManager.Registry.Rebuild (monitor);
            } else {
                AddinManager.Registry.Update (monitor);
            }
        }

        public static void RegisterAddinServices ()
        {
            extension_nodes = AddinManager.GetExtensionNodes ("/Banshee/ServiceManager/Service");
        }

        public static void RegisterDefaultServices ()
        {
            RegisterService<DBusServiceManager> ();
            RegisterService<DBusCommandService> ();
            RegisterService<BansheeDbConnection> ();
            RegisterService<Banshee.Preferences.PreferenceService> ();
            // HACK: the next line shouldn't be here, it's needed to work around
            // a race in NDesk DBus. See bgo#627441.
            RegisterService<Banshee.Networking.Network> ();
            RegisterService<SourceManager> ();
            RegisterService<MediaProfileManager> ();
            RegisterService<PlayerEngineService> ();
            RegisterService<PlaybackControllerService> ();
            RegisterService<JobScheduler> ();
            RegisterService<Banshee.Hardware.HardwareManager> ();
            RegisterService<Banshee.Collection.Indexer.CollectionIndexerService> ();
            RegisterService<Banshee.Metadata.SaveTrackMetadataService> ();
        }

        public static void DefaultInitialize ()
        {
            Initialize ();
            InitializeAddins ();
            RegisterDefaultServices ();
            RegisterAddinServices ();
        }

        private static void OnClientStarted (Client client)
        {
            DelayedInitialize ();
        }

        public static void Run()
        {
            lock (self_mutex) {
                OnStartupBegin ();

                uint cumulative_timer_id = Log.InformationTimerStart ();

                System.Net.ServicePointManager.DefaultConnectionLimit = 6;

                foreach (Type type in service_types) {
                    RegisterService (type);
                }

                if (extension_nodes != null) {
                    foreach (TypeExtensionNode node in extension_nodes) {
                        StartExtension (node);
                    }
                }

                if (AddinManager.IsInitialized) {
                    AddinManager.AddExtensionNodeHandler ("/Banshee/ServiceManager/Service", OnExtensionChanged);
                }

                is_initialized = true;

                Log.InformationTimerPrint (cumulative_timer_id, "All services are started {0}");

                OnStartupFinished ();
            }
        }

        private static IService RegisterService (Type type)
        {
            IService service = null;

            try {
                uint timer_id = Log.DebugTimerStart ();
                service = (IService)Activator.CreateInstance (type);
                RegisterService (service);

                Log.DebugTimerPrint (timer_id, String.Format (
                    "Core service started ({0}, {{0}})", service.ServiceName));

                OnServiceStarted (service);

                if (service is IDisposable) {
                    dispose_services.Push (service);
                }

                if (service is IInitializeService) {
                    ((IInitializeService)service).Initialize ();
                }

                return service;
            } catch (Exception e) {
                if (service is IRequiredService) {
                    Log.ErrorFormat ("Error initializing required service {0}",
                            service == null ? type.ToString () : service.ServiceName, false);
                    throw;
                }

                Log.Warning (String.Format ("Service `{0}' not started: {1}", type.FullName,
                    e.InnerException != null ? e.InnerException.Message : e.Message));
                Log.Exception (e.InnerException ?? e);
            }

            return null;
        }

        private static void StartExtension (TypeExtensionNode node)
        {
            if (extension_services.ContainsKey (node.Path)) {
                return;
            }

            IExtensionService service = null;

            try {
                uint timer_id = Log.DebugTimerStart ();

                service = (IExtensionService)node.CreateInstance (typeof (IExtensionService));
                service.Initialize ();
                RegisterService (service);

                DelayedInitialize (service);

                Log.DebugTimerPrint (timer_id, String.Format (
                    "Extension service started ({0}, {{0}})", service.ServiceName));

                OnServiceStarted (service);

                extension_services.Add (node.Path, service);

                dispose_services.Push (service);
            } catch (Exception e) {
                Log.Exception (e.InnerException ?? e);
                Log.Warning (String.Format ("Extension `{0}' not started: {1}",
                    service == null ? node.Path : service.GetType ().FullName, e.Message));
            }
        }

        private static void OnExtensionChanged (object o, ExtensionNodeEventArgs args)
        {
            lock (self_mutex) {
                TypeExtensionNode node = (TypeExtensionNode)args.ExtensionNode;

                if (args.Change == ExtensionChange.Add) {
                    StartExtension (node);
                } else if (args.Change == ExtensionChange.Remove && extension_services.ContainsKey (node.Path)) {
                    IExtensionService service = extension_services[node.Path];
                    extension_services.Remove (node.Path);
                    Remove (service);
                    ((IDisposable)service).Dispose ();

                    Log.DebugFormat ("Extension service disposed ({0})", service.ServiceName);

                    // Rebuild the dispose stack excluding the extension service
                    IService [] tmp_services = new IService[dispose_services.Count - 1];
                    int count = tmp_services.Length;
                    foreach (IService tmp_service in dispose_services) {
                        if (tmp_service != service) {
                            tmp_services[--count] = tmp_service;
                        }
                    }
                    dispose_services = new Stack<IService> (tmp_services);
                }
            }
        }

        private static bool delayed_initialized, have_client;
        private static void DelayedInitialize ()
        {
            lock (self_mutex) {
                if (!delayed_initialized) {
                    have_client = true;
                    var initialized = new HashSet <string> ();
                    var to_initialize = services.Values.ToList ();
                    foreach (IService service in to_initialize) {
                        if (!initialized.Contains (service.ServiceName)) {
                            DelayedInitialize (service);
                            initialized.Add (service.ServiceName);
                        }
                    }
                    delayed_initialized = true;
                }
            }
        }

        private static void DelayedInitialize (IService service)
        {
            try {
                if (have_client && service is IDelayedInitializeService) {
                    Log.DebugFormat ("Delayed Initializating {0}", service);
                    ((IDelayedInitializeService)service).DelayedInitialize ();
                }
            } catch (Exception e) {
                Log.Exception (e.InnerException ?? e);
                Log.Warning (String.Format ("Service `{0}' not initialized: {1}",
                    service.GetType ().FullName, e.Message));
            }
        }

        public static void Shutdown ()
        {
            lock (self_mutex) {
                while (dispose_services.Count > 0) {
                    IService service = dispose_services.Pop ();
                    try {
                        ((IDisposable)service).Dispose ();
                        Log.DebugFormat ("Service disposed ({0})", service.ServiceName);
                    } catch (Exception e) {
                        Log.Exception (String.Format ("Service disposal ({0}) threw an exception", service.ServiceName), e);
                    }
                }

                services.Clear ();
            }
        }

        public static void RegisterService (IService service)
        {
            lock (self_mutex) {
                Add (service);

                if(service is IRemoteExportable) {
                    RemoteServiceManager.RegisterObject ((IRemoteExportable)service);
                }
            }
        }

        public static void RegisterService<T> () where T : IService
        {
            lock (self_mutex) {
                if (is_initialized) {
                    RegisterService (Activator.CreateInstance <T> ());
                } else {
                    service_types.Add (typeof (T));
                }
            }
        }

        public static bool Contains (string serviceName)
        {
            lock (self_mutex) {
                return services.ContainsKey (serviceName);
            }
        }

        public static bool Contains<T> () where T : class, IService
        {
            return Contains (typeof (T).Name);
        }

        public static IService Get (string serviceName)
        {
            lock (self_mutex) {
                if (services.ContainsKey (serviceName)) {
                    return services[serviceName];
                }
            }

            return null;
        }

        public static T Get<T> () where T : class, IService
        {
            lock (self_mutex) {
                Type type = typeof (T);
                T service = Get (type.Name) as T;
                if (service == null && typeof(IRegisterOnDemandService).IsAssignableFrom (type)) {
                    return RegisterService (type) as T;
                }

                return service;
            }
        }

        private static void Add (IService service)
        {
            services.Add (service.ServiceName, service);
            //because Get<T>() works this way:
            var type_name = service.GetType ().Name;
            if (type_name != service.ServiceName) {
                services.Add (type_name, service);
            }
        }

        private static void Remove (IService service)
        {
            services.Remove (service.ServiceName);
            //because Add () works this way:
            services.Remove (service.GetType ().Name);
        }

        private static void OnStartupBegin ()
        {
            EventHandler handler = StartupBegin;
            if (handler != null) {
                handler (null, EventArgs.Empty);
            }
        }

        private static void OnStartupFinished ()
        {
            EventHandler handler = StartupFinished;
            if (handler != null) {
                handler (null, EventArgs.Empty);
            }
        }

        private static void OnServiceStarted (IService service)
        {
            ServiceStartedHandler handler = ServiceStarted;
            if (handler != null) {
                handler (new ServiceStartedArgs (service));
            }
        }

        public static int StartupServiceCount {
            get { return service_types.Count + (extension_nodes == null ? 0 : extension_nodes.Count) + 1; }
        }

        public static bool IsInitialized {
            get { return is_initialized; }
        }

        public static DBusServiceManager DBusServiceManager {
            get { return Get<DBusServiceManager> (); }
        }

        public static BansheeDbConnection DbConnection {
            get { return (BansheeDbConnection)Get ("DbConnection"); }
        }

        public static MediaProfileManager MediaProfileManager {
            get { return Get<MediaProfileManager> (); }
        }

        public static SourceManager SourceManager {
            get { return (SourceManager)Get ("SourceManager"); }
        }

        public static JobScheduler JobScheduler {
            get { return (JobScheduler)Get ("JobScheduler"); }
        }

        public static PlayerEngineService PlayerEngine {
            get { return (PlayerEngineService)Get ("PlayerEngine"); }
        }

        public static PlaybackControllerService PlaybackController {
            get { return (PlaybackControllerService)Get ("PlaybackController"); }
        }

        public static HardwareManager HardwareManager {
            get { return Get<HardwareManager> (); }
        }
    }
}
