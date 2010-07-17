//
// GioDriveMetadetaSource.cs
//
// Author:
//   alex <${AuthorEmail}>
//
// Copyright (c) 2010 alex
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

using GLib;

namespace Banshee.Hardware.Gio
{
    public class GioDriveMetadetaSource : GioMetadataSource
    {
        Drive Drive { get; set; }

        public GioDriveMetadetaSource (Drive drive)
        {
            Drive = drive;
        }

#region implemented abstract members of Banshee.Hardware.Gio.GioMetadataSource
        public override string GetPropertyString (string key)
        {
            throw new System.NotImplementedException();
        }

        public override double GetPropertyDouble (string key)
        {
            throw new System.NotImplementedException();
        }

        public override bool GetPropertyBoolean (string key)
        {
            throw new System.NotImplementedException();
        }

        public override int GetPropertyInteger (string key)
        {
            throw new System.NotImplementedException();
        }

        public override ulong GetPropertyUInt64 (string key)
        {
            throw new System.NotImplementedException();
        }

        public override string[] GetPropertyStringList (string key)
        {
            throw new System.NotImplementedException();
        }

        public override bool PropertyExists (string key)
        {
            throw new System.NotImplementedException();
        }
#endregion
    }
}
#endif
