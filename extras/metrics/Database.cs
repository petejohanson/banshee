//
// Database.cs
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

using Hyena;
using Hyena.Metrics;
using Hyena.Data.Sqlite;
using Hyena.Json;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.GZip;

namespace metrics
{
    public class Config
    {
        [DatabaseColumn (Constraints = DatabaseColumnConstraints.PrimaryKey)]
        public long Id;

        [DatabaseColumn]
        public string Key;

        [DatabaseColumn]
        public string Value;
    }

    public class Database : HyenaSqliteConnection
    {
        public Database (string db_path) : base (db_path)
        {
            HyenaSqliteCommand.LogAll = ApplicationContext.CommandLine.Contains ("debug-sql");
            Execute ("PRAGMA cache_size = ?", 32768 * 4);
            Execute ("PRAGMA synchronous = OFF");
            Execute ("PRAGMA temp_store = MEMORY");
            Execute ("PRAGMA count_changes = OFF");
            Execute ("PRAGMA journal_mode = TRUNCATE");

            Config = new SqliteModelProvider<Config> (this, "Config", true);
            SampleProvider = new SqliteModelProvider<MultiUserSample> (this, "Samples", true);
            MetricProvider = new SqliteModelProvider<Metric> (this, "Metrics", true);
            Users = new SqliteModelProvider<User> (this, "Users", true);

            Execute ("CREATE INDEX IF NOT EXISTS SampleUserMetricIndex ON Samples (UserID, MetricID)");
        }

        public SqliteModelProvider<Config> Config { get; private set; }
        public SqliteModelProvider<MultiUserSample> SampleProvider { get; private set; }
        public SqliteModelProvider<Metric> MetricProvider { get; private set; }
        public SqliteModelProvider<User> Users { get; private set; }

        private const string collapse_source_metric = "Banshee/Configuration/sources.";
        private static char [] collapse_source_chars = new char [] {'-', '/', '.', '_'};
        private Dictionary<string, Metric> metrics = new Dictionary<string, Metric> ();
        public Metric GetMetric (string name)
        {
            Metric metric;
            if (metrics.TryGetValue (name, out metric))
                return metric;

            metric = MetricProvider.FetchFirstMatching ("Name = ?", name);
            if (metric == null) {
                metric = new Metric () { Name = name };
                MetricProvider.Save (metric);
            }

            metrics[name] = metric;
            return metric;
        }

        private Dictionary<string, User> users = new Dictionary<string, User> ();
        public User GetUser (string guid)
        {
            User user;
            if (users.TryGetValue (guid, out user))
                return user;

            user = Users.FetchFirstMatching ("Guid = ?", guid);
            if (user == null) {
                user = new User () { Guid = guid };
                Users.Save (user);
            }

            users[guid] = user;
            return user;
        }

        public static bool Exists (string db_path)
        {
            return System.IO.File.Exists (db_path);
        }

        private Config LastReportNumber {
            get {
                return Config.FetchFirstMatching ("Key = 'LastReportNumber'") ?? new Config () { Key = "LastReportNumber", Value = "0" };
            }
        }

        private Regex report_number_regex = new Regex ("data/(.{24}).json.gz", RegexOptions.Compiled);

        public void Import ()
        {
            var db = this;
            var sample_provider = SampleProvider;

            var last_config = LastReportNumber;
            long last_report_number = Int64.Parse (last_config.Value);

            var max_report_size = 150 * 1024;

            db.BeginTransaction ();
            foreach (var file in System.IO.Directory.GetFiles ("data")) {
                var match = report_number_regex.Match (file);
                if (!match.Success) {
                    continue;
                }

                long num = Int64.Parse (match.Groups[1].Captures[0].Value);
                if (num <= last_report_number) {
                    continue;
                }

                var file_size = new System.IO.FileInfo (file).Length;
                if (file_size > max_report_size) {
                    Log.InformationFormat ("Skipping {0} because too large ({1:N0} KB compressed)", file, file_size/1024.0);
                    continue;
                }

                last_report_number = num;
                Log.DebugFormat ("Importing {0}", file);

                try {
                    JsonObject o = null;
                    using (var stream = System.IO.File.OpenRead (file)) {
                        using (var gzip_stream = new GZipInputStream (stream)) {
                            using (var txt_stream = new System.IO.StreamReader (gzip_stream)) {
                                o = new Deserializer (txt_stream.ReadToEnd ()).Deserialize () as JsonObject;
                            }
                        }
                    }

                    if (o == null)
                        throw new Exception ("Unable to parse JSON; empty file, maybe?");

                    string user_id = (string) o["ID"];
                    int format_version = (int) o["FormatVersion"];
                    if (format_version != MetricsCollection.FormatVersion) {
                        Log.WarningFormat ("Ignoring user report with old FormatVersion: {0}", format_version);
                        continue;
                    }

                    var metrics = o["Metrics"] as JsonObject;
                    foreach (string metric_name in metrics.Keys) {
                        // Skip these; they are a ton of data, and really more for debug purposes
                        if (metric_name == "Banshee/LongSqliteCommand")
                            continue;

                        var samples = metrics[metric_name] as JsonArray;

                        string name = metric_name;
                        if (name.StartsWith (collapse_source_metric)) {
                            string [] pieces = name.Split ('/');
                            var reduced_name = pieces[2].Substring (8, pieces[2].IndexOfAny (collapse_source_chars, 8) - 8);
                            name = String.Format ("{0}{1}/{2}", collapse_source_metric, reduced_name, pieces[pieces.Length - 1]);
                        }

                        foreach (JsonArray sample in samples) {
                            sample_provider.Save (MultiUserSample.Import (db, user_id, name, (string)sample[0], (object)sample[1]));
                        }
                    }
                    db.CommitTransaction ();
                } catch (Exception e) {
                    Log.Exception (String.Format ("Failed to read {0}", file), e);
                    db.RollbackTransaction ();
                }

                last_config.Value = last_report_number.ToString ();
                Config.Save (last_config);

                db.BeginTransaction ();
            }
            db.CommitTransaction ();

            last_config.Value = last_report_number.ToString ();
            Config.Save (last_config);

            Log.InformationFormat ("Done importing - last report # = {0}", last_report_number);
        }
    }

    [SqliteFunction (Name = "HYENA_METRICS_MEDIAN_LONG", FuncType = FunctionType.Aggregate, Arguments = 1)]
    internal class MedianFunctionLong : MedianFunction<long>
    {
    }

    [SqliteFunction (Name = "HYENA_METRICS_MEDIAN_DOUBLE", FuncType = FunctionType.Aggregate, Arguments = 1)]
    internal class MedianFunctionDouble : MedianFunction<double>
    {
    }

    [SqliteFunction (Name = "HYENA_METRICS_MEDIAN_DATETIME", FuncType = FunctionType.Aggregate, Arguments = 1)]
    internal class MedianFunctionDateTime : MedianFunction<DateTime>
    {
    }

    internal class MedianFunction<T> : SqliteFunction
    {
        public override void Step (object[] args, int stepNumber, ref object contextData)
        {
            List<T> list = null;
            if (contextData == null) {
                contextData = list = new List<T> ();
            } else {
                list = contextData as List<T>;
            }

            var val = (T)SqliteUtils.FromDbFormat (typeof(T), args[0]);
            list.Add (val);
        }

        public override object Final (object contextData)
        {
            var list = contextData as List<T>;
            if (list == null || list.Count == 0)
                return null;

            list.Sort ();
            return list[list.Count / 2];
        }
    }
}
