//
// Directory.cs
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
using System.Linq;
using System.Collections.Generic;

using GLib;

using Hyena;
using Banshee.Base;

namespace Banshee.IO.Gio
{
    public class Directory : IDirectory
    {
        public void Create (string directory)
        {
            var file = GetDir (directory);
            if (!file.Exists) {
                file.MakeDirectoryWithParents (null);
            }
        }

        public void Delete (string directory)
        {
            Delete (directory, false);
        }

        public void Delete (string directory, bool recursive)
        {
            Delete (directory, GetDir (directory), recursive);
        }

        internal static bool DisableNativeOptimizations = false;

        private void Delete (string directory, GLib.File dir, bool recursive)
        {
            if (!dir.Exists) {
                return;
            }

            if (dir.QueryFileType (FileQueryInfoFlags.NofollowSymlinks, null) != FileType.Directory) {
                return;
            }

            // If native, use the System.IO recursive delete
            if (dir.IsNative && !DisableNativeOptimizations) {
                System.IO.Directory.Delete (directory, recursive);
                return;
            }

            if (recursive) {
                foreach (string child in GetFiles (dir, false)) {
                    FileFactory.NewForUri (child).Delete ();
                }

                foreach (string child in GetDirectories (dir, false)) {
                    Delete (child, GetDir (child, true), true);
                }
            }

            dir.Delete ();
        }

        private static GLib.File GetDir (string directory)
        {
            return GetDir (directory, directory.Contains ("://"));
        }

        private static GLib.File GetDir (string directory, bool directoryIsUri)
        {
            return directoryIsUri ? FileFactory.NewForUri (directory) : FileFactory.NewForPath (directory);
        }

        public bool Exists (string directory)
        {
            var file = GetDir (directory);
            if (!file.QueryExists (null))
                return false;

            var type = file.QueryFileType (FileQueryInfoFlags.None, null);
            return (type & FileType.Directory) != 0;
        }

        public IEnumerable<string> GetFiles (string directory)
        {
            return GetFiles (GetDir (directory), true);
        }

        private IEnumerable<string> GetFiles (GLib.File dir, bool followSymlinks)
        {
            var enumerator = dir.EnumerateChildren ("standard::type,standard::name", followSymlinks ? FileQueryInfoFlags.None : FileQueryInfoFlags.NofollowSymlinks, null);
            foreach (FileInfo file in enumerator) {
                if ((file.FileType & FileType.Regular) != 0) {
                    var ret = dir.Uri.AbsoluteUri + "/" + Uri.EscapeDataString (file.Name);
                    file.Dispose ();
                    yield return ret;
                } else {
                    file.Dispose ();
                }
            }
            if (!enumerator.IsClosed) {
                enumerator.Close (null);
                enumerator.Dispose ();
            }
        }

        public IEnumerable<string> GetDirectories (string directory)
        {
            return GetDirectories (GetDir (directory), true);
        }

        private IEnumerable<string> GetDirectories (GLib.File dir, bool followSymlinks)
        {
            var enumerator = dir.EnumerateChildren ("standard::type,standard::name", followSymlinks ? FileQueryInfoFlags.None : FileQueryInfoFlags.NofollowSymlinks, null);
            foreach (FileInfo file in enumerator) {
                if ((file.FileType & FileType.Directory) != 0) {
                    var ret = dir.Uri.AbsoluteUri + "/" + Uri.EscapeDataString (file.Name);
                    file.Dispose ();
                    yield return ret;
                } else {
                    file.Dispose ();
                }
            }
            if (!enumerator.IsClosed) {
                enumerator.Close (null);
                enumerator.Dispose ();
            }
        }

        public void Move (SafeUri from, SafeUri to)
        {
            var dir = FileFactory.NewForUri (from.AbsoluteUri);
            dir.Move (FileFactory.NewForUri (to.AbsoluteUri), FileCopyFlags.None, null, null);
        }
    }
}
