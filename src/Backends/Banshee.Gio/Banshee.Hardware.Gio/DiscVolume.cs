//
// DiskVolume.cs
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

using Banshee.Hardware;

namespace Banshee.Hardware.Gio
{
    class DiscVolume : Volume, IDiscVolume
    {
        public static new IDiscVolume Resolve (IDevice device)
        {
            var raw = device as IRawDevice;
            if (raw != null && raw.Device is RawVolume) {
                return new DiscVolume (raw.Device);
            }

            return null;
        }

        public bool HasAudio {
            get {
                return PropertyExists ("ID_CDROM_MEDIA_TRACK_COUNT_AUDIO");
            }
        }

        public bool HasData {
            get {
                return PropertyExists ("ID_CDROM_MEDIA_TRACK_COUNT_DATA");
            }
        }

        public bool IsBlank {
            get {
                return GetPropertyString ("ID_CDROM_MEDIA_STATE") == "blank";
            }
        }

        public bool IsRewritable {
            get {
                return PropertyExists ("ID_CDROM_MEDIA_CD_RW");
            }
        }

        public ulong MediaCapacity {
            get {
                return GetPropertyUInt64 ("size");
            }
        }

        DiscVolume (RawDevice device)
            : base (device)
        {

        }
    }
}
#endif
