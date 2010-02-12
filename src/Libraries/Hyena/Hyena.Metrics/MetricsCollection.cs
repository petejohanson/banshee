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
using System.Reflection;

using Hyena.Json;

namespace Hyena.Metrics
{
    public sealed class MetricsCollection : List<Metric>, IDisposable
    {
        public string AnonymousUserId { get; private set; }
        public ISampleStore Store { get; private set; }

        public MetricsCollection (string uniqueUserId, ISampleStore store)
        {
            AnonymousUserId = uniqueUserId;
            Store = store;
        }

        public Metric Add (string category, string metricName, Func<object> sampleFunc)
        {
            return Add (category, metricName, sampleFunc, false);
        }

        public Metric Add (string category, string metricName, Func<object> sampleFunc, bool isEventDriven)
        {
            var metric = new Metric (category, metricName, Store, sampleFunc, isEventDriven);
            Add (metric);
            return metric;
        }

        public void Dispose ()
        {
            foreach (var m in this) {
                m.Dispose ();
            }
            Clear ();
        }

        public override string ToString ()
        {
            var sb = new StringBuilder ();

            // TODO handle dates in a culture-invariant manner
            sb.AppendFormat ("ID: {0}\n", AnonymousUserId);
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

        public string ToJsonString ()
        {
            var report = new Dictionary<string, object> ();

            report["ID"] = AnonymousUserId;
            report["Metrics"] = this.GroupBy<Metric, string> (m => m.Category)
                .Select (c => {
                    var d = new Dictionary<string, object> ();
                    foreach (var metric in c) {
                        d[metric.Name] = Store.GetFor (metric).Select (s =>
                            new object [] { s.Stamp, s.Value }
                        );
                    }
                    return d;
                });

            return report.ToJsonString ();
        }

        public void AddDefaults ()
        {
            Add ("Env", "OS Platform",          () => PlatformDetection.SystemName);
            Add ("Env", "OS Version",           () => System.Environment.OSVersion);
            Add ("Env", "Processor Count",      () => System.Environment.ProcessorCount);
            Add ("Env", ".NET Runtime Version", () => System.Environment.Version);
            Add ("Env", "Debugging",            () => ApplicationContext.Debugging);
            Add ("Env", "CultureInfo",          () => System.Globalization.CultureInfo.CurrentCulture.Name);

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ()) {
                var name = asm.GetName ();
                Add ("Assemblies", name.Name, () => name.Version);
            }
        }
    }
}