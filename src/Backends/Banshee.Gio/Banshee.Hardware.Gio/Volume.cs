//
// Volume.cs
//
// Author:
//   Alan McGovern <amcgovern@novell.com>
//
// Copyright (c) 2010 Moonlight Team
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
    class Volume : Device, IVolume
    {
        RawVolume volume;

        public static new IVolume Resolve (IDevice device)
        {
            var raw = device as IRawDevice;
            if (raw != null && raw.Device is RawVolume) {
                return new Volume (raw.Device);
            }
            return null;
        }

        public string DeviceNode {
            get { return device.DeviceNode; }
        }

        public string MountPoint {
            get { return volume.MountPoint; }
        }

        public bool IsMounted {
            get { return volume.IsMounted; }
        }
        
        public bool IsReadOnly {
            get { return volume.IsReadOnly; }
        }

        public ulong Capacity {
            get { return volume.Capacity; }
        }

        public long Available {
            get { return volume.Available; }
        }

        public IBlockDevice Parent {
            get {
                return BlockDevice.Resolve (this);
            }
        }

        public bool ShouldIgnore {
            get { return false; }
        }

        public string FileSystem {
            get { return volume.FileSystem; }
        }

        public bool CanEject {
            get { return volume.CanEject; }
        }

        public bool CanMount {
            get { return volume.CanMount; }
        }
        public bool CanUnmount {
            get { return volume.CanUnmount; }
        }

        public Volume (RawDevice device)
            : base (device)
        {
            volume = (RawVolume) device;
        }

        public void Eject ()
        {
            volume.Eject ();
        }

        public void Mount ()
        {
            volume.Mount ();
        }

        public void Unmount ()
        {
            volume.Unmount ();
        }
    }
}

#endif
