//
// DkDisk.cs
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

using NDesk.DBus;

namespace Banshee.HalBackend
{
    public class DkDisk
    {
        public static DkDisk FindByDevice (string device_path)
        {
            if (device_path == null)
                return null;

            if (disks == null)
                return null;


            string disk_path = null;
            try {
                disk_path = disks.FindDeviceByDeviceFile (device_path);
            } catch {}

            if (disk_path == null)
                return null;

            try {
                return new DkDisk (disk_path);
            } catch {}

            return null;
        }

        private IDkDisk disk;
        private org.freedesktop.DBus.Properties props;

        private DkDisk (string obj_path)
        {
            disk = Bus.System.GetObject<IDkDisk>("org.freedesktop.DeviceKit.Disks",
                new ObjectPath(obj_path));

            props = Bus.System.GetObject<org.freedesktop.DBus.Properties>("org.freedesktop.DeviceKit.Disks",
                new ObjectPath(obj_path));
        }

        public bool IsMounted {
            get {
                return (bool) props.Get ("org.freedesktop.DeviceKit.Disks.Device", "DeviceIsMounted");
            }
        }

        public bool IsReadOnly {
            get {
                return (bool) props.Get ("org.freedesktop.DeviceKit.Disks.Device", "DeviceIsReadOnly");
            }
        }

        public string MountPoint {
            get {
                var ary = (string[])props.Get ("org.freedesktop.DeviceKit.Disks.Device", "DeviceMountPaths");
                return ary != null && ary.Length > 0 ? ary[0] : null;
            }
        }

        public void Eject ()
        {
            disk.DriveEject (new string [0]);
        }

        public void Unmount ()
        {
            disk.FilesystemUnmount (new string [0]);
        }

        private static IDkDisks disks;

        static DkDisk ()
        {
            try {
                disks = Bus.System.GetObject<IDkDisks>("org.freedesktop.DeviceKit.Disks",
                    new ObjectPath("/org/freedesktop/DeviceKit/Disks"));
            } catch {}
        }

        [Interface("org.freedesktop.DeviceKit.Disks")]
        internal interface IDkDisks
        {
            string FindDeviceByDeviceFile (string deviceFile);
        }

    }

    [Interface("org.freedesktop.DeviceKit.Disks.Device")]
    public interface IDkDisk
    {
        bool DeviceIsMounted { get; }
        string [] DeviceMountPaths { get; }
        void DriveEject (string [] options);
        void FilesystemUnmount (string [] options);
    }
}
