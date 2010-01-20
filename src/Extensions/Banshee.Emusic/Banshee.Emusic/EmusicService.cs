//
// EmusicService.cs
//
// Author:
//   Eitan Isaacson <eitan@monotonous.org>
//
// Copyright (C) 2009 Eitan Isaacson
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
using System.IO;
using System.Xml;
using System.Collections.Generic;
using Mono.Unix;

using Hyena;

using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Library;
using Banshee.Base;
using Banshee.Gui;

using Migo.DownloadCore;
using Migo.TaskCore;

namespace Banshee.Emusic
{
    public class EmusicService : IExtensionService, IDisposable
    {
        private DownloadManager download_manager;
        private DownloadManagerInterface download_manager_iface;
        private LibraryImportManager import_manager;
        private Dictionary<string,HttpFileDownloadTask> tasks;
        private readonly string tmp_download_path = Paths.Combine (Paths.ExtensionCacheRoot, "emusic", "partial-downloads");

        public EmusicService ()
        {
        }

        public void Initialize ()
        {
            tasks = new Dictionary<string, HttpFileDownloadTask> ();

            import_manager = new LibraryImportManager (true);
            import_manager.ImportResult += HandleImportResult;

            if (download_manager == null)
                download_manager = new DownloadManager (2, tmp_download_path);

            if (download_manager_iface == null) {
                download_manager_iface = new DownloadManagerInterface (download_manager);
                download_manager_iface.Initialize ();
            }

            ServiceManager.Get<DBusCommandService> ().ArgumentPushed += OnCommandLineArgument;
        }

        public void Dispose ()
        {
            ServiceManager.Get<DBusCommandService> ().ArgumentPushed -= OnCommandLineArgument;
            download_manager.Dispose ();
            download_manager_iface.Dispose ();
            download_manager = null;
            download_manager_iface = null;
        }

        string IService.ServiceName {
            get { return "EmusicService"; }
        }

        private void OnCommandLineArgument (string argument, object value, bool isFile)
        {
            if (isFile && File.Exists (argument) && Path.GetExtension (argument) == ".emx")
                ImportEmx (argument);
        }

        public void ImportEmx (params string[] uris)
        {
            foreach (string uri in uris) {
                using (var xml_reader = new XmlTextReader (uri)) {
                    while (xml_reader.Read()) {
                        if (xml_reader.NodeType == XmlNodeType.Element &&
                            xml_reader.Name == "TRACKURL")
                        {
                            xml_reader.Read();
                            Hyena.Log.DebugFormat ("Downloading: {0}", xml_reader.Value);
                            HttpFileDownloadTask task = download_manager.CreateDownloadTask (xml_reader.Value);

                            if (File.Exists (task.LocalPath)) {
                                File.Delete (task.LocalPath); // FIXME: We go into a download loop if we don't.
                            }

                            task.Completed += OnDownloadCompleted;
                            download_manager.QueueDownload (task);
                            tasks.Add (task.LocalPath, task);
                        }
                    }
                }
            }
        }

        private void OnDownloadCompleted (object sender, TaskCompletedEventArgs args)
        {
            HttpFileDownloadTask task = sender as HttpFileDownloadTask;

            if (task.Status != TaskStatus.Succeeded) {
                task.Completed -= OnDownloadCompleted;

                if (File.Exists (task.LocalPath)) {
                    File.Delete (task.LocalPath);
                }

                if (Directory.Exists (Path.GetDirectoryName (task.LocalPath))) {
                    Directory.Delete (Path.GetDirectoryName (task.LocalPath));
                }

                tasks.Remove (task.LocalPath);

                Hyena.Log.ErrorFormat ("Could not download eMusic track: {0}",
                                       task.Error.ToString());
            } else {
                import_manager.Enqueue (task.LocalPath);
            }
        }

        void HandleImportResult(object o, DatabaseImportResultArgs args)
        {
            if (tasks.ContainsKey (args.Path)) {
                HttpFileDownloadTask task = tasks[args.Path];
                task.Completed -= OnDownloadCompleted;
                File.Delete (args.Path);
                Directory.Delete (Path.GetDirectoryName (args.Path));
                tasks.Remove (args.Path);
            }
        }

    }
}
