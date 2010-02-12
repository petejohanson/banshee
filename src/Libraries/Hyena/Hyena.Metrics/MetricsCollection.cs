//
// MetricsCollection.cs
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
using System.Collections.Generic;
using System.Text;

namespace Hyena.Metrics
{
    public sealed class MetricsCollection : List<Metric>
    {
        public string UniqueUserId { get; private set; }
        public ISampleStore Store { get; private set; }

        public MetricsCollection (string uniqueUserId, ISampleStore store)
        {
            UniqueUserId = uniqueUserId;
            Store = store;
        }

        public void Add (string category, string metricName, Func<object> sampleFunc)
        {
            Add (new Metric (category, metricName, Store, sampleFunc));
        }

        public override string ToString ()
        {
            var sb = new StringBuilder ();

            sb.AppendFormat ("ID: {0}\n", UniqueUserId);
            foreach (var category in this.GroupBy<Metric, string> (m => m.Category)) {
                sb.AppendFormat ("{0}:\n", category.Key);
                foreach (var metric in category) {
                    sb.AppendFormat ("  {0}\n", metric.Name);
                    foreach (var sample in Store.GetFor (metric)) {
                        sb.AppendFormat ("    {0}\n", sample.Value);
                    }
                }
            }

            return sb.ToString ();
        }

        public void AddDefaults ()
        {
            Add ("Env", "OS Platform",          () => PlatformDetection.SystemName);
            Add ("Env", "OS Version",           () => System.Environment.OSVersion);
            Add ("Env", "Processor Count",      () => System.Environment.ProcessorCount);
            Add ("Env", ".NET Runtime Version", () => System.Environment.Version);
            Add ("Env", "Debugging",            () => ApplicationContext.Debugging);
            Add ("Env", "CultureInfo",          () => System.Globalization.CultureInfo.CurrentCulture.Name);
        }
    }
}
