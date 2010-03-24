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

            if (udisks_finder == null && dk_finder == null)
                return null;


            string disk_path = null;
            try {
                if (udisks_finder != null) {
                    disk_path = udisks_finder.FindDeviceByDeviceFile (device_path);
                } else {
                    disk_path = dk_finder.FindDeviceByDeviceFile (device_path);
                }
            } catch {}

            if (disk_path == null)
                return null;

            try {
                return new DkDisk (disk_path);
            } catch {}

            return null;
        }

        private UDisksDisk udisks_disk;
        private IDkDisk dk_disk;
        private org.freedesktop.DBus.Properties props;

        const string dk_bus_name = "org.freedesktop.DeviceKit.Disks";
        const string udisks_bus_name = "org.freedesktop.UDisks";

        private DkDisk (string obj_path)
        {
            if (udisks_finder != null) {
                udisks_disk = Bus.System.GetObject<UDisksDisk> (udisks_bus_name, new ObjectPath (obj_path));
                props = Bus.System.GetObject<org.freedesktop.DBus.Properties> (udisks_bus_name, new ObjectPath (obj_path));
            } else {
                dk_disk = Bus.System.GetObject<IDkDisk> (dk_bus_name, new ObjectPath(obj_path));
                props = Bus.System.GetObject<org.freedesktop.DBus.Properties> (dk_bus_name, new ObjectPath(obj_path));
            }
        }

        public bool IsMounted {
            get {
                return (bool) props.Get (props_iface, "DeviceIsMounted");
            }
        }

        public bool IsReadOnly {
            get {
                return (bool) props.Get (props_iface, "DeviceIsReadOnly");
            }
        }

        public string MountPoint {
            get {
                var ary = (string[])props.Get (props_iface, "DeviceMountPaths");
                return ary != null && ary.Length > 0 ? ary[0] : null;
            }
        }

        public void Eject ()
        {
            if (udisks_disk != null) {
                udisks_disk.DriveEject (new string [0]);
            } else {
                dk_disk.DriveEject (new string [0]);
            }
        }

        public void Unmount ()
        {
            if (udisks_disk != null) {
                udisks_disk.FilesystemUnmount (new string [0]);
            } else {
                dk_disk.FilesystemUnmount (new string [0]);
            }
        }

        private static UDisksFinder udisks_finder;
        private static DkFinder dk_finder;
        private static string props_iface;

        static DkDisk ()
        {
            try {
                udisks_finder = Bus.System.GetObject<UDisksFinder>(udisks_bus_name, new ObjectPath("/org/freedesktop/UDisks"));
                props_iface = "org.freedesktop.UDisks.Device";
            } catch {
                try {
                    dk_finder = Bus.System.GetObject<DkFinder>(dk_bus_name,
                        new ObjectPath("/org/freedesktop/DeviceKit/Disks"));
                    props_iface = "org.freedesktop.DeviceKit.Disks.Device";
                } catch {}
            }
        }

        [Interface("org.freedesktop.UDisks")]
        internal interface UDisksFinder
        {
            string FindDeviceByDeviceFile (string deviceFile);
        }

        [Interface("org.freedesktop.DeviceKit.Disks")]
        internal interface DkFinder
        {
            string FindDeviceByDeviceFile (string deviceFile);
        }

        [Interface("org.freedesktop.UDisks.Device")]
        internal interface UDisksDisk
        {
            bool DeviceIsMounted { get; }
            string [] DeviceMountPaths { get; }
            void DriveEject (string [] options);
            void FilesystemUnmount (string [] options);
        }

        [Interface("org.freedesktop.DeviceKit.Disks.Device")]
        internal interface IDkDisk
        {
            bool DeviceIsMounted { get; }
            string [] DeviceMountPaths { get; }
            void DriveEject (string [] options);
            void FilesystemUnmount (string [] options);
        }
    }
}
