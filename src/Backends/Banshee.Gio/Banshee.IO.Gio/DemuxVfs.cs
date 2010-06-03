//
// DemuxVfs.cs
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
using GLib;

using Banshee.Base;

namespace Banshee.IO.Gio
{
    public class DemuxVfs : IDemuxVfs
    {
        private GLib.File file;

        public DemuxVfs (string path)
        {
            file = path.StartsWith ("/") ? FileFactory.NewForPath (path) : FileFactory.NewForUri (path);
            if (file.Exists) {
                using (var info = file.QueryInfo ("etag::value,access::can-read,access::can-write", FileQueryInfoFlags.None, null)) {
                    IsReadable = info.GetAttributeBoolean ("access::can-read");
                    IsWritable = info.GetAttributeBoolean ("access::can-write");
                }
            }
        }

        public void CloseStream (System.IO.Stream stream)
        {
            stream.Close ();
        }

        public string Name {
            get { return file.ParsedName; }
        }

        public System.IO.Stream ReadStream {
            get { return new GioStream (file.Read (null)); }
        }

        public System.IO.Stream WriteStream {
            get { return new GioStream (file.Exists
                    ? file.OpenReadwrite (null)
                    : file.CreateReadwrite (FileCreateFlags.None, null)
                );
            }
        }

        public bool IsReadable { get; private set; }
        public bool IsWritable { get; private set; }
    }
}
