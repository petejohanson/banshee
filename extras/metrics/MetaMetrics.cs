//
// MetaMetrics.cs
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
using Hyena.Data.Sqlite;
using System.Collections;

namespace metrics
{
    public class SampleModel : ICacheableDatabaseModel
    {
        public string ReloadFragment { get; set; }
        public string SelectAggregates { get; set; }
        public string JoinTable { get; set; }
        public string JoinFragment { get; set; }
        public string JoinPrimaryKey { get; set; }
        public string JoinColumn { get; set; }
        public bool CachesJoinTableEntries { get; set; }
        public bool CachesValues { get; set; }

        public int FetchCount {
            get { return 10; }
        }
        public Hyena.Collections.Selection Selection { get; private set; }

        public SqliteModelCache<MultiUserSample> Cache { get; private set; }

        private static int id;

        public SampleModel (string condition, Database db, string aggregates)
        {
            Selection = new Hyena.Collections.Selection ();
            ReloadFragment = String.Format ("FROM Samples {0}", condition);
            SelectAggregates = aggregates;
            Cache = new SqliteModelCache<MultiUserSample> (db, (id++).ToString (), this, db.SampleProvider);
        }

        public void Reload ()
        {
            Cache.Reload ();
            Cache.UpdateAggregates ();
        }
    }

    public class MetricSampleModel : SampleModel
    {
        private Metric metric;
        public string MetricName { get; private set; }
        public long MetricId { get { return metric.Id; } }

        private string condition;
        public MetricSampleModel (SqliteModelCache<MultiUserSample> limiter, Database db, string aggregates) : base (null, db, aggregates)
        {
            condition = String.Format (
                "FROM Samples, HyenaCache WHERE Samples.MetricID = {0} AND HyenaCache.ModelID = {1} AND Samples.ID = HyenaCache.ItemID",
                "{0}", limiter.CacheId
            );
        }

        public void ChangeMetric (Database db, string metricName)
        {
            MetricName = metricName;
            metric = db.GetMetric (metricName);
            ReloadFragment = String.Format (condition, metric.Id);
            Reload ();
        }
    }

    public class MetaMetrics
    {
        string fmt = "{0,20}";

        public MetaMetrics (Database db)
        {
            var latest_samples = new SampleModel ("GROUP BY UserID, MetricID ORDER BY stamp desc", db, "COUNT(DISTINCT(UserID)), MIN(Stamp), MAX(Stamp)");
            latest_samples.Cache.AggregatesUpdated += (reader) => {
                Console.WriteLine ("Total unique users for this time slice: {0}", reader[1]);
                Console.WriteLine ("First report was on {0}", SqliteUtils.FromDbFormat (typeof(DateTime), reader[2]));
                Console.WriteLine ("Last report was on {0}", SqliteUtils.FromDbFormat (typeof(DateTime), reader[3]));
                Console.WriteLine ();
            };
            latest_samples.Reload ();

            var string_summary = new MetricSampleModel (latest_samples.Cache, db,
                @"COUNT(DISTINCT(UserID))"
            );
            string_summary.Cache.AggregatesUpdated += (agg_reader) => {
                Console.WriteLine (String.Format ("   Users:  {0}", fmt), agg_reader[1]);
                using (var reader = new HyenaDataReader (db.Query (
                    @"SELECT COUNT(DISTINCT(UserId)) as users, Value FROM Samples, HyenaCache
                        WHERE MetricId = ? AND HyenaCache.ModelID = ? AND HyenaCache.ItemID = Samples.ID
                        GROUP BY Value ORDER BY users DESC", string_summary.MetricId, string_summary.Cache.CacheId))) {
                    while (reader.Read ()) {
                        Console.WriteLine ("   {0,-5}: {1,-20}", reader.Get<long> (0), reader.Get<string> (1));
                    }
                }
                Console.WriteLine ();
            };

            var numeric_slice = new MetricSampleModel (latest_samples.Cache, db,
                @"MIN(CAST(Value as NUMERIC)), MAX(CAST(Value as NUMERIC)),
                  AVG(CAST(Value as NUMERIC)), HYENA_METRICS_MEDIAN_DOUBLE(CAST(Value as NUMERIC)), COUNT(DISTINCT(UserID))"
            );

            numeric_slice.Cache.AggregatesUpdated += (reader) => {
                Console.WriteLine (String.Format ("   Users:  {0}", fmt), reader[5]);
                Console.WriteLine (String.Format ("   Min:    {0}", fmt), Metric.ToString (numeric_slice.MetricName, reader[1]));
                Console.WriteLine (String.Format ("   Avg:    {0}", fmt), Metric.ToString (numeric_slice.MetricName, reader[3]));
                Console.WriteLine (String.Format ("   Median: {0}", fmt), Metric.ToString (numeric_slice.MetricName, reader[4]));
                Console.WriteLine (String.Format ("   Max:    {0}", fmt), Metric.ToString (numeric_slice.MetricName, reader[2]));
                Console.WriteLine ();
            };
            
            var metrics = db.QueryEnumerable<string> ("SELECT Name FROM Metrics ORDER BY Name ASC");
            foreach (var metric in metrics) {
                switch (GetMetricType (metric)) {
                case "string":
                    Console.WriteLine ("{0}:", metric);
                    string_summary.ChangeMetric (db, metric);
                    break;
                //case "timespan" : SummarizeNumeric<TimeSpan> (metric); break;
                //case "datetime" : SummarizeNumeric<DateTime> (metric); break;
                case "float":
                    Console.WriteLine ("{0}:", metric);
                    //SummarizeNumeric<long> (metric_cache);
                    numeric_slice.ChangeMetric (db, metric);
                    break;
                //case "float":
                    //SummarizeNumeric<double> (metric_cache);
                    //break;
                }
            }
        }

        private string GetMetricType (string name)
        {
            var lower_name = name.ToLower ();
            foreach (var str in new string [] { "avg", "count", "size", "width", "height", "duration", "playseconds", "_pos" }) {
                if (lower_name.Contains (str))
                    return "float";
            }

            if (name.EndsWith ("BuildTime"))
                return "datetime";

            if (name.EndsWith ("LongSqliteCommand") || name.EndsWith ("At") || name.StartsWith ("Assemblies/") ||
                    name.EndsWith ("child_sort_id") || name.EndsWith ("separate_by_type") || name.EndsWith ("expanded"))
                return null;

            return "string";
        }
    }
}
