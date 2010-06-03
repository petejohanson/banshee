//
// Main.cs
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
using Hyena.CommandLine;

namespace metrics
{
    public class MainEntry
    {
        const string db_path = "data/metrics.db";

        public static void Main (string [] args)
        {
            try {
                using (var db = new Database (db_path)) {
                    if (args != null && args.Length > 0 && args[0] == "--timeline") {
                        var metric = db.GetMetric ("Banshee/StartedAt");
                        var usage_samples = new SampleModel (String.Format ("WHERE MetricId = {0}", metric.Id), db, "1");
                        usage_samples.Reload ();

                        for (long i = 0; i < usage_samples.Cache.Count; i++) {
                            var sample = usage_samples.Cache.GetValue (i);
                            Console.WriteLine (
                                "{1} {0} {2} {0} {3} {0} {4}",
                                "<TUFTE>", DateTimeUtil.FromDateTime (sample.Stamp), sample.UserId, sample.CacheEntryId, sample.CacheEntryId
                            );
                        }

                        return;
                    }

                    db.Import ();
                    new MetaMetrics (db);
                }
            } catch (Exception e) {
                Console.WriteLine ("Going down, got exception {0}", e);
                throw;
            }
        }
    }
}
