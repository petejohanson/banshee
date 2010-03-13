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
using System.Linq;

using Hyena;
using Hyena.Metrics;
using Hyena.Data.Sqlite;
using Hyena.Json;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using Hyena.Query;

namespace metrics
{
    public class Metric
    {
        [DatabaseColumn (Constraints = DatabaseColumnConstraints.PrimaryKey)]
        public long Id { get; private set; }

        [DatabaseColumn (Index = "MetricNameIndex")]
        public string Name { get; set; }

        public Metric () {}

        static Metric ()
        {
            var time = new Func<double, string> (d => String.Format ("{0:N0}", SqliteUtils.FromDbFormat (typeof(DateTime), d)));
            var duration = new Func<double, string> (d => String.Format ("{0:N0}", TimeSpan.FromMilliseconds (d)));
            var duration_s = new Func<double, string> (d => String.Format ("{0:N0}", TimeSpan.FromSeconds (d)));
            var px = new Func<double, string> (d => String.Format ("{0:N0} px", d));

            Add (
                "/AvgBitRate", new Func<double, string> (d => String.Format ("{0:N0} kbps", d)),
                "/AvgScore",
                "/BpmTrackCount",
                "/ComposerTrackCount",
                "/ErrorTrackCount",
                "/GroupingTrackCount",
                "/LicenseUriTrackCount",
                "/RatedTrackCount",
                "/TotalFileSize", new Func<double, string> (d => new FileSizeQueryValue ((long)d).ToUserQuery ()),
                "/TotalPlayCount",
                "/TotalPlaySeconds", duration_s,
                "/TotalSkipCount",
                "/TrackCount",
                "/UnplayedTrackCount",

                "Banshee/BuildTime", time,
                "Banshee/Configuration/browser/position", px,

                "Banshee/Configuration/player_window/height", px,
                "Banshee/Configuration/player_window/source_view_row_height", px,
                "Banshee/Configuration/player_window/source_view_row_padding", px,
                "Banshee/Configuration/player_window/source_view_width", px,
                "Banshee/Configuration/player_window/width", px,

                "Banshee/Configuration/player_window/x_pos",
                "Banshee/Configuration/player_window/y_pos",

                "Banshee/Configuration/plugins.mtp/albumart_max_width", px,

                "Banshee/Configuration/plugins.play_queue/played_songs_number",
                "Banshee/Configuration/plugins.play_queue/upcoming_songs_number",

                "Banshee/Display/NScreens",

                "Banshee/Screen/Height", px,
                "Banshee/Screen/Width", px,
                "Banshee/Screen/NMonitors",
                "Banshee/ShutdownAt", time,
                "Banshee/StartedAt", time,
                "Env/Processor Count",

                "Banshee/RunDuration", duration
            );
        }

        private static List<Metric> metrics = new List<Metric> ();
        private static Func<double, string> num_func = new Func<double, string> (d => String.Format ("{0,20:N1}", d));

        private static void Add (params object [] args)
        {
            for (int i = 0; i < args.Length; i++) {
                string key = (string)args[i];
                Func<double, string> func = null;
                if (i < args.Length - 1) {
                    func = args[i + 1] as Func<double, string>;
                    if (func != null) {
                        i++;
                    }
                }

                metrics.Add (new Metric (key, func ?? num_func));
            }
        }

        public static string ToString (string key, object d)
        {
            return ToString (key, Convert.ToDouble (d));
        }

        public static string ToString (string key, double d)
        {
            var metric = metrics.FirstOrDefault (m => m.Matching (key));
            if (metric != null) {
                return metric.ToString (d);
            } else {
                return num_func (d);
            }
        }

        //private string key;
        private bool ends_with;
        private Func<double, string> func;

        public Metric (string key, Func<double, string> func)
        {
            Name = key;
            this.func = func;
            this.ends_with = key[0] == '/';
        }

        public string ToString (double val)
        {
            try {
                return func (val);
            } catch (Exception e) {
                return e.Message;
            }
        }

        public bool Matching (string key)
        {
            if (ends_with) {
                return key.EndsWith (Name);
            } else {
                return key == Name;
            }
        }
    }
}
