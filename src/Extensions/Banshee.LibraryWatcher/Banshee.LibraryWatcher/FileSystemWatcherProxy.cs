//
// FileSystemWatcherProxy.cs
//
// Authors:
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//
// Copyright (C) 2009 Alexander Kojevnikov
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

namespace Banshee.LibraryWatcher
{
    public class FileSystemWatcherProxy : IDisposable
    {
        private readonly Banshee.LibraryWatcher.IO.FileSystemWatcher watcher_fixed;
        private readonly System.IO.FileSystemWatcher watcher;

        public FileSystemWatcherProxy(string path)
        {
            // FIXME: There must be a better way to check if we are running under Linux.
            if (Environment.OSVersion.Platform == PlatformID.Unix &&
                Environment.OSVersion.Version.ToString ().StartsWith ("2.6.")) {
                watcher_fixed = new Banshee.LibraryWatcher.IO.FileSystemWatcher (path);
            } else {
                watcher = new System.IO.FileSystemWatcher (path);
            }
        }

        public void Dispose ()
        {
            if (watcher_fixed != null) {
                watcher_fixed.Dispose ();
            } else {
                watcher.Dispose ();
            }
        }

        public bool IncludeSubdirectories {
            set {
                if (watcher_fixed != null) {
                    watcher_fixed.IncludeSubdirectories = value;
                } else {
                    watcher.IncludeSubdirectories = value;
                }
            }
        }

        public event System.IO.FileSystemEventHandler Changed {
            add {
                if (watcher_fixed != null) {
                    watcher_fixed.Changed += value;
                } else {
                    watcher.Changed += value;
                }
            }
            remove {
                if (watcher_fixed != null) {
                    watcher_fixed.Changed -= value;
                } else {
                    watcher.Changed -= value;
                }
            }
        }

        public event System.IO.FileSystemEventHandler Created {
            add {
                if (watcher_fixed != null) {
                    watcher_fixed.Created += value;
                } else {
                    watcher.Created += value;
                }
            }
            remove {
                if (watcher_fixed != null) {
                    watcher_fixed.Created -= value;
                } else {
                    watcher.Created -= value;
                }
            }
        }

        public event System.IO.FileSystemEventHandler Deleted {
            add {
                if (watcher_fixed != null) {
                    watcher_fixed.Deleted += value;
                } else {
                    watcher.Deleted += value;
                }
            }
            remove {
                if (watcher_fixed != null) {
                    watcher_fixed.Deleted -= value;
                } else {
                    watcher.Deleted -= value;
                }
            }
        }

        public event System.IO.RenamedEventHandler Renamed {
            add {
                if (watcher_fixed != null) {
                    watcher_fixed.Renamed += value;
                } else {
                    watcher.Renamed += value;
                }
            }
            remove {
                if (watcher_fixed != null) {
                    watcher_fixed.Renamed -= value;
                } else {
                    watcher.Renamed -= value;
                }
            }
        }

        public bool EnableRaisingEvents {
            set {
                if (watcher_fixed != null) {
                    watcher_fixed.EnableRaisingEvents = value;
                } else {
                    watcher.EnableRaisingEvents = value;
                }
            }
        }
    }
}
