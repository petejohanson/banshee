//
// BansheeMetrics.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (c) 2010 Novell, Inc.
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
using System.Linq;
using Mono.Unix;

using Hyena;
using Hyena.Metrics;

using Banshee.Configuration;
using Banshee.ServiceStack;
using System.Reflection;

namespace Banshee.Metrics
{
    public class BansheeMetrics : IDisposable
    {
        private static BansheeMetrics banshee_metrics;
        public BansheeMetrics Instance { get { return banshee_metrics; } }

        public static void Start ()
        {
            Log.Information ("Starting collection of anonymous usage data");
            if (banshee_metrics == null) {
                banshee_metrics = new BansheeMetrics ();
            }
        }

        public static void Stop ()
        {
            Log.Information ("Stopping collection of anonymous usage data");
            if (banshee_metrics != null) {
                banshee_metrics.Dispose ();
                banshee_metrics = null;
            }
        }

        private MetricsCollection metrics;
        private string id_key = "AnonymousUsageData.Userid";

        private Metric shutdown, duration;

        private BansheeMetrics ()
        {
            string unique_userid = DatabaseConfigurationClient.Client.Get<string> (id_key, null);

            if (unique_userid == null) {
                unique_userid = System.Guid.NewGuid ().ToString ();
                DatabaseConfigurationClient.Client.Set<string> (id_key, unique_userid);
            }

            metrics = new MetricsCollection (unique_userid, new DbSampleStore (
                ServiceManager.DbConnection, "AnonymousUsageData"
            ));
            metrics.AddDefaults ();

            // TODO add more Banshee-specific metrics
            Add ("Client",       () => Application.ActiveClient);
            Add ("BuildHostCpu", () => Application.BuildHostCpu);
            Add ("BuildHostOS",  () => Application.BuildHostOperatingSystem);
            Add ("BuildTime",    () => Application.BuildTime);
            Add ("BuildVendor",  () => Application.BuildVendor);
            Add ("Version",      () => Application.Version);
            Add ("StartedAt",    () => ApplicationContext.StartedAt);

            // TODO add metrics based on assemblies/versions; put in AddDefaults?

            shutdown = Add ("ShutdownAt",  () => DateTime.Now, true);
            duration = Add ("RunDuration", () => DateTime.Now - ApplicationContext.StartedAt, true);
            Application.ShutdownRequested += OnShutdownRequested;

            // TODO add Mono.Addins extension point for metric providers?
            // TODO schedule sending the data to the server in some timeout?

            // TODO remove this, just for testing
            Log.InformationFormat ("Anonymous usage data collected:\n{0}", metrics.ToString ());
        }

        private bool OnShutdownRequested ()
        {
            try {
                shutdown.TakeSample ();
                duration.TakeSample ();
            } catch {
            } finally {
                return true;
            }
        }

        public Metric Add (string name, Func<object> func)
        {
            return Add (name, func, null);
        }

        public Metric Add (string name, Func<object> func, EventInfo triggerEvent)
        {
            return metrics.Add ("Banshee", name, func, triggerEvent);
        }

        public void Dispose ()
        {
            // Disconnect from events we're listening to
            Application.ShutdownRequested -= OnShutdownRequested;

            // Delete any collected data
            metrics.Store.Clear ();
            metrics.Dispose ();
            metrics = null;

            // Forget the user's unique id
            DatabaseConfigurationClient.Client.Set<string> (id_key, null);
        }

        public static SchemaEntry<bool> EnableCollection = new SchemaEntry<bool> (
            "core", "send_anonymous_usage_data", false, // disabled by default
            Catalog.GetString ("Improve Banshee by sending anonymous usage data"), null
        );
    }
}