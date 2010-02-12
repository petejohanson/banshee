//
// Metric.cs
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
using System.Collections.Generic;
using System.Reflection;

namespace Hyena.Metrics
{
    public sealed class Metric : IDisposable
    {
        public string FullName {
            get { return String.Format ("{0}.{1}", Category, Name); }
        }

        public string Category { get; private set; }
        public string Name { get; private set; }
        public bool IsEventDriven { get; private set; }

        private ISampleStore store;
        private Func<object> sample_func;

        internal Metric (string category, string name, ISampleStore store, Func<object> sampleFunc, bool isEventDriven)
        {
            Category = category;
            Name = name;
            this.store = store;
            sample_func = sampleFunc;
            IsEventDriven = isEventDriven;

            if (!isEventDriven) {
                // Take the sample and forget the delegate so it can be GC'd
                TakeSample ();
                sample_func = null;
            }
        }

        public void Dispose ()
        {
        }

        public void PushSample (object sampleValue)
        {
            try {
                store.Add (new Sample (this, sampleValue));
            } catch (Exception e) {
                Log.Exception ("Error taking sample", e);
            }
        }

        public void TakeSample ()
        {
            if (sample_func == null) {
                Log.Warning ("sampleFunc is null.  Are you calling TakeSample on a non-event-driven metric?");
                return;
            }

            try {
                store.Add (new Sample (this, sample_func ()));
            } catch (Exception e) {
                Log.Exception ("Error taking sample", e);
            }
        }
    }
}
