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
using Banshee.Sources;
using Hyena.Data.Sqlite;

namespace Banshee.Metrics
{
    public class BansheeMetrics : IDisposable
    {
        private static BansheeMetrics banshee_metrics;

        public BansheeMetrics Instance { get { return banshee_metrics; } }

        public static event System.Action Started;
        public static event System.Action Stopped;

        public static void Start ()
        {
            Log.Information ("Starting collection of anonymous usage data");
            if (banshee_metrics == null) {
                try {
                    banshee_metrics = new BansheeMetrics ();
                } catch (Exception e) {
                    Hyena.Log.Exception ("Failed to start collection of anonymous usage data", e);
                    banshee_metrics = null;
                }
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

        private Metric shutdown, duration, source_changed, sqlite_executed;

        private BansheeMetrics ()
        {
            string unique_userid = DatabaseConfigurationClient.Client.Get<string> (id_key, null);

            if (String.IsNullOrEmpty (unique_userid)) {
                unique_userid = System.Guid.NewGuid ().ToString ();
                DatabaseConfigurationClient.Client.Set<string> (id_key, unique_userid);
            }

            metrics = new MetricsCollection (unique_userid, new DbSampleStore (
                ServiceManager.DbConnection, "AnonymousUsageData"
            ));

            if (Application.ActiveClient != null && Application.ActiveClient.IsStarted) {
                Initialize (null);
            } else {
                Application.ClientStarted += Initialize;
            }
        }

        private void Initialize (Client client)
        {
            Application.ClientStarted -= Initialize;

            metrics.AddDefaults ();
            AddMetrics ();

            var handler = Started;
            if (handler != null) {
                handler ();
            }

            // TODO schedule sending the data to the server in some timeout?

            // TODO remove this, just for testing
            Log.InformationFormat ("Anonymous usage data collected:\n{0}", metrics.ToJsonString ());
            //System.IO.File.WriteAllText ("usage-data.json", metrics.ToJsonString ());
        }

        private void AddMetrics ()
        {
            Add ("Client",       () => Application.ActiveClient);
            Add ("BuildHostCpu", () => Application.BuildHostCpu);
            Add ("BuildHostOS",  () => Application.BuildHostOperatingSystem);
            Add ("BuildTime",    () => Application.BuildTime);
            Add ("BuildVendor",  () => Application.BuildVendor);
            Add ("Version",      () => Application.Version);
            Add ("StartedAt",    () => ApplicationContext.StartedAt);

            Console.WriteLine ("SourceMgr is null? {0}", ServiceManager.SourceManager == null);
            foreach (var src in ServiceManager.SourceManager.FindSources<PrimarySource> ()) {
                var type_name = src.TypeName;
                var reader = new HyenaDataReader (ServiceManager.DbConnection.Query (
                    @"SELECT COUNT(*),
                             COUNT(CASE ifnull(Rating, 0)        WHEN 0 THEN NULL ELSE 1 END),
                             COUNT(CASE ifnull(BPM, 0)           WHEN 0 THEN NULL ELSE 1 END),
                             COUNT(CASE ifnull(LastStreamError, 0) WHEN 0 THEN NULL ELSE 1 END),
                             COUNT(CASE ifnull(Grouping, 0)      WHEN 0 THEN NULL ELSE 1 END),
                             SUM(PlayCount),
                             SUM(SkipCount),
                             CAST (SUM(PlayCount * (Duration/1000)) AS INTEGER),
                             SUM(FileSize)
                    FROM CoreTracks WHERE PrimarySourceID = ?", src.DbId
                ));

                // DateAdded, Grouping
                var results = new string [] {
                    "TrackCount", "RatedTrackCount", "BpmTrackCount", "ErrorTrackCount",
                    "GroupingTrackCount", "TotalPlayCount", "TotalSkipCount", "TotalPlaySeconds", "TotalFileSize"
                };

                for (int i = 0; i < results.Length; i++) {
                    metrics.Add (type_name, results[i], () => reader.Get<long> (i));
                }
                reader.Dispose ();
            }

            source_changed = Add ("ActiveSourceChanged", () => ServiceManager.SourceManager.ActiveSource.TypeName, true);
            ServiceManager.SourceManager.ActiveSourceChanged += OnActiveSourceChanged;

            shutdown = Add ("ShutdownAt",  () => DateTime.Now, true);
            duration = Add ("RunDuration", () => DateTime.Now - ApplicationContext.StartedAt, true);
            Application.ShutdownRequested += OnShutdownRequested;

            sqlite_executed = Add ("LongSqliteCommand", null, true);
            HyenaSqliteCommand.CommandExecuted += OnSqliteCommandExecuted;
            HyenaSqliteCommand.RaiseCommandExecuted = true;
            HyenaSqliteCommand.RaiseCommandExecutedThresholdMs = 400;
        }

        public Metric Add (string name, Func<object> func)
        {
            return Add (name, func, false);
        }

        public Metric Add (string name, Func<object> func, bool isEventDriven)
        {
            return metrics.Add ("Banshee", name, func, isEventDriven);
        }

        public void Dispose ()
        {
            var handler = Stopped;
            if (handler != null) {
                handler ();
            }

            // Disconnect from events we're listening to
            ServiceManager.SourceManager.ActiveSourceChanged -= OnActiveSourceChanged;
            Application.ShutdownRequested -= OnShutdownRequested;
            HyenaSqliteCommand.CommandExecuted -= OnSqliteCommandExecuted;

            // Delete any collected data
            metrics.Store.Clear ();
            metrics.Dispose ();
            metrics = null;

            // Forget the user's unique id
            DatabaseConfigurationClient.Client.Set<string> (id_key, "");
        }

        #region Event Handlers

        private void OnActiveSourceChanged (SourceEventArgs args)
        {
            source_changed.TakeSample ();
        }

        private bool OnShutdownRequested ()
        {
            shutdown.TakeSample ();
            duration.TakeSample ();
            return true;
        }

        private void OnSqliteCommandExecuted (object o, CommandExecutedArgs args)
        {
            sqlite_executed.PushSample (String.Format ("{0}ms -- {1}", args.Ms, args.Sql));
        }

        #endregion

        public static SchemaEntry<bool> EnableCollection = new SchemaEntry<bool> (
            "core", "send_anonymous_usage_data", false, // disabled by default
            Catalog.GetString ("Improve Banshee by sending anonymous usage data"), null
        );
    }
}