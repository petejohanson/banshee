//
// Volume.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright 2011 Novell, Inc.
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

using System;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using Banshee.Hardware;

namespace Banshee.Windows.Hardware
{
    public class Volume : Device, IVolume
    {
        // Regex to grab the alphanumeric code before the ending "&0".  Example inputs:
        //  USBSTOR\DISK&VEN_HTC&PROD_ANDROID_PHONE&REV_0100\8&5901EA1&0&HT851N003062&0
        //  USBSTOR\DISK&VEN_KINGSTON&PROD_DATATRAVELER_2.0&REV_1.00\89900000000000006CB02B99&0
        static Regex regex = new Regex ("[^a-zA-Z0-9]([a-zA-Z0-9]+)&0$", RegexOptions.Compiled);

        // Expects a Win32_DiskDrive object
        // http://msdn.microsoft.com/en-us/library/aa394132%28v=VS.85%29.aspx
        internal Volume (ManagementObject o)
        {
            if (o.ClassPath.ClassName != "Win32_DiskDrive")
                throw new ArgumentException (o.ClassPath.ClassName, "o");

            Uuid = o.Str ("PNPDeviceID");
            Name = o.Str ("Caption");

            // Get USB vendor/product ids from the associated CIM_USBDevice
            // This way of associating them (via a substring of the PNPDeviceID) is quite a hack; patches welcome
            var match = regex.Match (Uuid);
            if (match.Success) {
                string query = String.Format ("SELECT * FROM CIM_USBDevice WHERE DeviceID LIKE '%{0}'", match.Groups[1].Captures[0].Value);
                UsbDevice = HardwareManager.Query (query).Select (u => new UsbDevice (u)).FirstOrDefault ();
            }

            // Get MountPoint and more from the associated LogicalDisk
            // FIXME this assumes one partition for the device
            foreach (ManagementObject partition in o.GetRelated ("Win32_DiskPartition")) {
                foreach (ManagementObject disk in partition.GetRelated ("Win32_LogicalDisk")) {
                    //IsMounted = (bool)ld.GetPropertyValue ("Automount") == true;
                    //IsReadOnly = (ushort) ld.GetPropertyValue ("Access") == 1;
                    MountPoint = disk.Str ("Name") + "/";
                    FileSystem = disk.Str ("FileSystem");
                    Capacity = (ulong) disk.GetPropertyValue ("Size");
                    Available = (long)(ulong)disk.GetPropertyValue ("FreeSpace");
                    return;
                }
            }
        }

        public string DeviceNode { get; set; }
        public string MountPoint { get; set; }
        public bool IsMounted { get; set; }
        public bool IsReadOnly { get; set; }
        public ulong Capacity { get; set; }
        public long Available { get; set; }
        public IBlockDevice Parent { get; set; }
        public bool ShouldIgnore { get; set; }
        public string FileSystem { get; set; }

        public bool CanEject { get; set; }
        public void Eject () {}

        public bool CanMount { get; set; }
        public void Mount () {}

        public bool CanUnmount { get; set; }
        public void Unmount () {}
    }
}
