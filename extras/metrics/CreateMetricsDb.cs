//
// CreateMetricsDb.cs
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
    public class Database
    {
        const string db_path = "metrics.db";

        public static HyenaSqliteConnection Open ()
        {
            return new HyenaSqliteConnection (db_path);
        }

        public static bool Exists { get { return System.IO.File.Exists (db_path); } }

        public static void Import ()
        {
            using (var db = Open ()) {
                MultiUserSample.Provider = new SqliteModelProvider<MultiUserSample> (db, "Samples", true);
                foreach (var file in System.IO.Directory.GetFiles ("data")) {
                    Log.InformationFormat ("Importing {0}", file);

                    try {
                        var o = new Deserializer (System.IO.File.ReadAllText (file)).Deserialize () as JsonObject;

                        string user_id = (string) o["ID"];
                        int format_version = (int) o["FormatVersion"];
                        if (format_version != MetricsCollection.FormatVersion) {
                            Log.WarningFormat ("Ignoring user report with old FormatVersion: {0}", format_version);
                            continue;
                        }

                        var metrics = o["Metrics"] as JsonObject;
                        db.BeginTransaction ();
                        try {
                            foreach (string metric_name in metrics.Keys) {
                                var samples = metrics[metric_name] as JsonArray;
                                foreach (JsonArray sample in samples) {
                                    MultiUserSample.Import (user_id, metric_name, (string)sample[0], (object)sample[1]);
                                }
                            }
                            db.CommitTransaction ();
                        } catch {
                            db.RollbackTransaction ();
                            throw;
                        }
                    } catch (Exception e) {
                        Log.Exception (String.Format ("Failed to read {0}", file), e);
                    }
                }
            }
        }
    }
}