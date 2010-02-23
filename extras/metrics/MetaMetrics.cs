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
    public class MetaMetrics
    {
        private HyenaSqliteConnection db;

        public DateTime FirstReport { get; private set; }
        public DateTime LastReport  { get; private set; }

        public MetaMetrics (HyenaSqliteConnection db)
        {
            this.db = db;
            FirstReport = db.Query<DateTime> ("SELECT Stamp FROM Samples ORDER BY STAMP ASC");
            LastReport  = db.Query<DateTime> ("SELECT Stamp FROM Samples ORDER BY STAMP DESC");

            Console.WriteLine ("First report was on {0}", FirstReport);
            Console.WriteLine ("Last report was on {0}", LastReport);
            Console.WriteLine ("Total unique users: {0}", db.Query<long> ("SELECT COUNT(DISTINCT(UserId)) FROM Samples"));

            var metrics = db.QueryEnumerable<string> ("SELECT DISTINCT(MetricName) as name FROM Samples ORDER BY name ASC");

            foreach (var metric in metrics) {
                //var pieces = metric.Split ('/');
                //var name = pieces[pieces.Length - 1];

                switch (GetMetricType (metric)) {
                    case "string": SummarizeTextual (metric); break;
                    case "timespan" : SummarizeNumeric<TimeSpan> (metric); break;
                    case "datetime" : SummarizeNumeric<DateTime> (metric); break;
                    case "fixed": SummarizeNumeric<long> (metric); break;
                    case "float": SummarizeNumeric<double> (metric); break;
                }
            }

            /*SummarizeTextual ("Env/CultureInfo");
            SummarizeTextual ("Banshee/Configuration/core/io_provider");

            SummarizeNumeric<TimeSpan> ("Banshee/RunDuration");

            SummarizeNumeric<long> ("Banshee/Screen/Width");
            SummarizeNumeric<long> ("Banshee/Screen/Height");
            SummarizeNumeric<long> ("Banshee/Screen/NMonitors");
            SummarizeTextual ("Banshee/Screen/IsComposited");*/

            /*foreach (var metric in db.QueryEnumerable<string> ("SELECT DISTINCT(MetricName) as name FROM Samples ORDER BY name ASC")) {
                Console.WriteLine (metric);
            }*/
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

            if (name.EndsWith ("LongSqliteCommand") || name.EndsWith ("At"))
                return null;

            return "string";
        }

        private void SummarizeNumeric<T> (string metric_name)
        {
            Console.WriteLine ("{0}:", metric_name);
            string fmt = typeof(T) == typeof(DateTime) ? "{0}" : "{0,-20:N1}";
            Console.WriteLine (String.Format ("   Min: {0}", fmt), db.Query<T> ("SELECT MIN(CAST(Value as NUMERIC)) FROM Samples WHERE MetricName = ?", metric_name));
            Console.WriteLine (String.Format ("   Max: {0}", fmt), db.Query<T> ("SELECT MAX(CAST(Value as NUMERIC)) FROM Samples WHERE MetricName = ?", metric_name));
            Console.WriteLine (String.Format ("   Avg: {0}", fmt), db.Query<T> ("SELECT AVG(CAST(Value as NUMERIC)) FROM Samples WHERE MetricName = ?", metric_name));
            Console.WriteLine ();
        }

        private void SummarizeTextual (string metric_name)
        {
            Console.WriteLine ("{0}:", metric_name);
            //Console.WriteLine ("   Unique Values: {0,-20}", db.Query<long> ("SELECT COUNT(DISTINCT(Value)) FROM Samples WHERE MetricName = ?", metric_name));
            using (var reader = new HyenaDataReader (db.Query ("SELECT COUNT(DISTINCT(UserId)) as users, Value FROM Samples WHERE MetricName = ? GROUP BY Value ORDER BY users DESC", metric_name))) {
                while (reader.Read ()) {
                    Console.WriteLine ("   {0,-5}: {1,-20}", reader.Get<long> (0), reader.Get<string> (1));
                }
            }
            Console.WriteLine ();
        }
    }
}