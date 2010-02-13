// 
// MultiUserSample.cs
// 
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
// 
// Copyright (c) 2010 Gabriel Burt
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
    public class MultiUserSample : Sample
    {
        public static SqliteModelProvider<MultiUserSample> Provider;

        [DatabaseColumn]
        public string UserId;

        public MultiUserSample ()
        {
        }

        public static void Import (string user_id, string metric_name, string stamp, object val)
        {
            var sample = new MultiUserSample ();
            sample.UserId = user_id;
            sample.MetricName = metric_name;

            DateTime stamp_dt;
            if (DateTimeUtil.TryParseInvariant (stamp, out stamp_dt)) {
                sample.Stamp = stamp_dt;
            }

            DateTime value_dt;
            if (DateTimeUtil.TryParseInvariant (val as string, out value_dt)) {
                // We want numeric dates to compare with
                sample.Value = DateTimeUtil.ToTimeT (value_dt).ToString ();
            } else {
                sample.SetValue (val);
            }

            Provider.Save (sample);
        }
    }
}