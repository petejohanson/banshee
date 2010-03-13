//
// MultiUserSample.cs
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

namespace metrics
{
    public class MultiUserSample : Sample, Hyena.Data.ICacheableItem
    {
        [DatabaseColumn (Index = "SampleUserIdIndex")]
        public long UserId;

        [DatabaseColumn (Index = "SampleMetricIdIndex")]
        public long MetricId;

        // ICacheableItem
        public object CacheEntryId { get; set; }
        public long CacheModelId { get; set; }

        public MultiUserSample ()
        {
        }

        static DateTime value_dt;
        static TimeSpan value_span;
        public static MultiUserSample Import (Database db, string user_id, string metric_name, string stamp, object val)
        {
            var sample = new MultiUserSample ();
            sample.UserId = db.GetUser (user_id).Id;

            // TODO collapse various DAP and DAAP library stats?
            sample.MetricId = db.GetMetric (metric_name).Id;

            DateTime stamp_dt;
            if (!DateTimeUtil.TryParseInvariant (stamp, out stamp_dt)) {
                Hyena.Log.Error ("Invalid stamp: ", stamp);
                return null;
            }

            sample.Stamp = stamp_dt;

            string value_str = val as string;
            if (value_str != null) {
                if (DateTimeUtil.TryParseInvariant (val as string, out value_dt)) {
                    // We want numeric dates to compare with
                    sample.Value = DateTimeUtil.ToTimeT (value_dt).ToString ();
                } else if (value_str.Contains (":") && TimeSpan.TryParse (val as string, out value_span)) {
                    sample.Value = value_span.TotalMilliseconds.ToString ();
                }
            }

            if (sample.Value == null) {
                sample.SetValue (val);
            }

            return sample;
        }
    }
}
