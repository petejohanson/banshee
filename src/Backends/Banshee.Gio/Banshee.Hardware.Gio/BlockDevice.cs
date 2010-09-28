//
// BlockDevice.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Banshee.Hardware;

namespace Banshee.Hardware.Gio
{
    class BlockDevice : Device, IBlockDevice
    {
        // It's a block device if DEVTYPE is 'disk'
        const string UdevDeviceType = "DEVTYPE";

        public static new IBlockDevice Resolve (IDevice device)
        {
            var raw = device as IRawDevice;
            if (device == null || raw.Device.UdevMetadata.GetPropertyString (UdevDeviceType) != "disk") {
                return null;
            }

            RawBlockDevice rawBlock = null;
            if (raw.Device is RawVolume) {
                rawBlock = (raw.Device as RawVolume).Parent;
            } else {
                rawBlock = raw.Device as RawBlockDevice;
            }

            if (rawBlock == null) {
                return null;
            } else {
                return (BlockDevice) CdromDevice.Resolve (device);
            }
        }

        public string DeviceNode {
            get { return device.DeviceNode; }
        }

        public bool IsRemovable {
            get { return device.IsRemovable; }
        }

        public IEnumerable<IVolume> Volumes {
            get { return this; }
        }

        protected BlockDevice (RawDevice device)
            : base (device)
        {
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        public IEnumerator<IVolume> GetEnumerator ()
        {
            var rawBlock = (device is RawVolume)
                ? (device as RawVolume).Parent
                : device as RawBlockDevice;
            if (rawBlock == null) {
                yield break;
            }

            foreach (Volume volume in rawBlock.Volumes) {
                yield return (IVolume) DiscVolume.Resolve (volume) ?? volume;
            }
        }
    }
}
#endif
