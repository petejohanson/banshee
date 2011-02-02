//
// DeviceMediaCapabilities.cs
//
// Author:
//   Alex Launi <alex.launi@gmail.com>
//
// Copyright (c) 2010 Alex Launi
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

#if ENABLE_GIO_HARDWARE
using System;
using System.Collections.Generic;
using System.Linq;

using KeyFile;

using Banshee.Base;
using Banshee.Hardware;
using Banshee.IO;

namespace Banshee.Hardware.Gio
{
    class DeviceMediaCapabilities : IDeviceMediaCapabilities
    {
        private GMpiFileInfo mpi;

        // TODO implement. The technique for this will be to write a private
        // MPI reader class that I can use to query the mpi file for `device'.
        // the MPI file is found using the ID_MEDIA_PLAYER udev property + .mpi
        // in /usr/[local/]share/.

        public DeviceMediaCapabilities (string idMediaPlayer)
        {
            mpi = new GMpiFileInfo (idMediaPlayer);
        }

        public string[] OutputFormats {
            get {
                return mpi.OutputFormats;
            }
        }

#region IDeviceMediaCapabilities implementation
        public string[] AudioFolders {
            get {
                return mpi.AudioFolders;
            }
        }

        // FIXME
        // MPI has no property for this yet
        public string CoverArtFileName {
            get {
                return "";
            }
        }

        // FIXME
        // MPI has no property for this yet
        public string CoverArtFileType {
            get {
                return "";
            }
        }

        // FIXME
        // MPI has no property for this yet
        public int CoverArtSize {
            get {
                return -1;
            }
        }


        public int FolderDepth {
            get {
                return mpi.FolderDepth;
            }
        }


        public bool IsType (string type)
        {
            return mpi.AccessProtocols.Contains (type);
        }


        public string[] PlaybackMimeTypes {
            get {
                return mpi.OutputFormats;
            }
        }


        public string[] PlaylistFormats {
            get {
                return mpi.PlaylistFormats;
            }
        }


        public string PlaylistPath {
            get {
                return mpi.PlaylistPath;
            }
        }

        // FIXME
        // MPI has no property for this yet
        public string[] VideoFolders {
            get {
                return new string[] {""};
            }
        }
#endregion
        private class GMpiFileInfo
        {
            private const char Seperator = ';';
            private const string MediaGroup = "Media";
            private const string DeviceGroup = "Device";
            private const string VendorGroup = "Vendor";
            private const string StorageGroup = "storage";
            private const string PlaylistGroup = "Playlist";

            private GKeyFile mpi_file;

            public GMpiFileInfo (string mediaPlayerId)
            {
                try {
                    mpi_file = new GKeyFile ();
                    mpi_file.ListSeparator = Seperator;
                    mpi_file.LoadFromDirs (String.Format ("{0}.mpi", mediaPlayerId),
                                           new string [] {"/usr/share/media-player-info",
                                                          "/usr/local/share/media-player-info"},
                                           null, Flags.None);
                } catch (GLib.GException) {
                    Hyena.Log.WarningFormat ("Failed to load media-player-info file for {0}",
                                             mediaPlayerId);
                }

                LoadProperties ();
            }

            public string PlaylistPath {
                get; private set;
            }

            public string[] AudioFolders {
                get; private set;
            }

            public int FolderDepth {
                get; private set;
            }

            public string[] AccessProtocols {
                get; private set;
            }

            public string[] OutputFormats {
                get; private set;
            }

            public string[] InputFormats {
                get; private set;
            }

            public string[] PlaylistFormats {
                get; private set;
            }

            private void LoadProperties ()
            {
                InitDefaults ();

                if (mpi_file == null) {
                    return;
                }

                if (mpi_file.HasGroup (StorageGroup)) {
                    if (mpi_file.HasKey (StorageGroup, "FolderDepth")) {
                        FolderDepth = mpi_file.GetInteger (StorageGroup, "FolderDepth");
                    }

                    if (mpi_file.HasKey (StorageGroup, "PlaylistPath")) {
                        PlaylistPath = mpi_file.GetString (StorageGroup, "PlaylistPath");
                    }

                    if (mpi_file.HasKey (StorageGroup, "AudioFolders")) {
                        AudioFolders = mpi_file.GetStringList (StorageGroup, "AudioFolders");
                    }
                }

                if (mpi_file.HasGroup (MediaGroup)) {
                    if (mpi_file.HasKey (MediaGroup, "InputFormats")) {
                        InputFormats = mpi_file.GetStringList (MediaGroup, "InputFormats");
                    }

                    if (mpi_file.HasKey (MediaGroup, "OutputFormats")) {
                        OutputFormats = mpi_file.GetStringList (MediaGroup, "OutputFormats");
                    }
                }

                if (mpi_file.HasGroup (PlaylistGroup) && mpi_file.HasKey (PlaylistGroup, "Formats")) {
                    PlaylistFormats = mpi_file.GetStringList (PlaylistGroup, "Formats") ?? new string [] {};
                }

                if (mpi_file.HasGroup (DeviceGroup) && mpi_file.HasKey (DeviceGroup, "AccessProtocols")) {
                    AccessProtocols = mpi_file.GetStringList (DeviceGroup, "AccessProtocols") ?? new string [] {};
                }
            }

            private void InitDefaults ()
            {
                FolderDepth = 0;
                PlaylistPath = "";
                AudioFolders = new string[] {};
                InputFormats = new string[] {};
                OutputFormats = new string[] {};
                PlaylistFormats = new string[] {};
                AccessProtocols = new string[] {};
            }
        }
    }
}
#endif
