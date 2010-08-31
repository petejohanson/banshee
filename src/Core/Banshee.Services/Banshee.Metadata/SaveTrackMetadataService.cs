//
// SaveTrackMetadataService.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Mono.Unix;

using Hyena.Jobs;
using Hyena.Data.Sqlite;

using Banshee.Streaming;
using Banshee.Collection.Database;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Configuration.Schema;
using Banshee.Preferences;

namespace Banshee.Metadata
{
    public class SaveTrackMetadataService : IService
    {
        public static SchemaPreference<bool> WriteMetadataEnabled = new SchemaPreference<bool> (
                LibrarySchema.WriteMetadata,
                Catalog.GetString ("Write _metadata to files"),
                Catalog.GetString ("Save tags and other metadata inside supported media files")
        );

        public static SchemaPreference<bool> WriteRatingsAndPlayCountsEnabled = new SchemaPreference<bool> (
                LibrarySchema.WriteRatingsAndPlayCounts,
                Catalog.GetString ("Write _ratings and play counts to files"),
                Catalog.GetString ("Enable this option to save rating and playcount metadata inside supported audio files"));

        public static SchemaPreference<bool> RenameEnabled = new SchemaPreference<bool> (
                LibrarySchema.MoveOnInfoSave,
                Catalog.GetString ("_Update file and folder names"),
                Catalog.GetString ("Rename files and folders according to media metadata")
        );

        private SaveTrackMetadataJob job;
        private object sync = new object ();
        private bool inited = false;
        private List<PrimarySource> sources = new List<PrimarySource> ();
        public IEnumerable<PrimarySource> Sources {
            get { return sources.AsReadOnly (); }
        }

        public SaveTrackMetadataService ()
        {
            Banshee.ServiceStack.Application.RunTimeout (10000, delegate {
                WriteMetadataEnabled.ValueChanged += OnEnabledChanged;
                WriteRatingsAndPlayCountsEnabled.ValueChanged += OnEnabledChanged;
                RenameEnabled.ValueChanged += OnEnabledChanged;

                foreach (var source in ServiceManager.SourceManager.Sources) {
                    AddPrimarySource (source);
                }

                ServiceManager.SourceManager.SourceAdded += (a) => AddPrimarySource (a.Source);
                ServiceManager.SourceManager.SourceRemoved += (a) => RemovePrimarySource (a.Source);
                Save ();

                inited = true;
                return false;
            });
        }

        private void AddPrimarySource (Source s)
        {
            PrimarySource p = s as PrimarySource;
            if (p != null && p.HasEditableTrackProperties) {
                sources.Add (p);
                p.TracksChanged += OnTracksChanged;
            }
        }

        private void RemovePrimarySource (Source s)
        {
            PrimarySource p = s as PrimarySource;
            if (p != null && sources.Remove (p)) {
                p.TracksChanged -= OnTracksChanged;
            }
        }

        private void RemovePrimarySources ()
        {
            foreach (var source in sources) {
                source.TracksChanged -= OnTracksChanged;
            }
            sources.Clear ();
        }

        public void Dispose ()
        {
            if (inited) {
                RemovePrimarySources ();

                if (job != null) {
                    ServiceManager.JobScheduler.Cancel (job);
                }
            }
        }

        private void Save ()
        {
            if (!(WriteMetadataEnabled.Value || WriteRatingsAndPlayCountsEnabled.Value || RenameEnabled.Value))
                return;

            lock (sync) {
                if (job != null) {
                    job.WriteMetadataEnabled = WriteMetadataEnabled.Value;
                    job.WriteRatingsAndPlayCountsEnabled = WriteRatingsAndPlayCountsEnabled.Value;
                    job.RenameEnabled = RenameEnabled.Value;
                } else {
                    var new_job = new SaveTrackMetadataJob () {
                        WriteMetadataEnabled = WriteMetadataEnabled.Value,
                        WriteRatingsAndPlayCountsEnabled = WriteRatingsAndPlayCountsEnabled.Value,
                        RenameEnabled = RenameEnabled.Value
                    };
                    new_job.Finished += delegate { lock (sync) { job = null; } };
                    job = new_job;
                    job.Register ();
                }
            }
        }

        private void OnTracksChanged (Source sender, TrackEventArgs args)
        {
            Save ();
        }

        private void OnEnabledChanged (Root pref)
        {
            if (WriteMetadataEnabled.Value || WriteRatingsAndPlayCountsEnabled.Value || RenameEnabled.Value) {
                Save ();
            } else {
                if (job != null) {
                    ServiceManager.JobScheduler.Cancel (job);
                }
            }
        }

        string IService.ServiceName {
            get { return "SaveTrackMetadataService"; }
        }

        // Reserve strings in preparation for the forthcoming string freeze.
        public void ReservedStrings ()
        {
            Catalog.GetString ("Write _ratings and play counts to files");
            Catalog.GetString ("Enable this option to save rating and play count metadata inside supported audio files whenever the rating is changed.");
            Catalog.GetString ("Import _ratings");
            Catalog.GetString ("Import play _counts");
        }
    }
}
