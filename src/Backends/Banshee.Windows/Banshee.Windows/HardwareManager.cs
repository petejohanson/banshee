//
// HardwareManager.cs
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
using System.Collections.Generic;

using Banshee.Hardware;
using System.Management;
using System.Globalization;

namespace Banshee.Windows
{
    public class UsbDevice : IUsbDevice
    {
        // Expects a CIM_USBDevice entry
        internal UsbDevice (ManagementObject o)
        {
            Uuid = o.Str ("DeviceID");
            int i = Uuid.IndexOf ("VID_");
            VendorId = Int32.Parse (Uuid.Substring (i + 4, 4), NumberStyles.HexNumber);
            i = Uuid.IndexOf ("PID_");
            ProductId = Int32.Parse (Uuid.Substring (i + 4, 4), NumberStyles.HexNumber);
        }

        public int ProductId { get; set; }
        public int VendorId { get; set; }
        public double Speed { get; set; }
        public double Version { get; set; }

        // IDevice
        public string Uuid { get; set; }
        public string Serial { get; set; }
        public string Name { get; set; }
        public string Product { get; set; }
        public string Vendor { get; set; }

        public IDeviceMediaCapabilities MediaCapabilities { get; set; }

        public virtual IUsbDevice ResolveRootUsbDevice ()
        {
            return null;
        }

        public IUsbPortInfo ResolveUsbPortInfo ()
        {
            return null;
        }
    }

    public class Volume : IVolume
    {
        UsbDevice usb_device;

        // Expects a Win32_DiskDrive object
        internal Volume (ManagementObject o)
        {
            Uuid = o.Str ("DeviceID");

            string cc = o.Str ("PNPDeviceID");
            int i =  cc.LastIndexOf ('&');
            if (i >= 0) {
                cc = cc.Substring (0, cc.LastIndexOf ('&'));
                i = cc.LastIndexOf ('&');
                if (i >= 0) {
                    cc = cc.Substring (i + 1, cc.Length - i - 1);
                    cc = HardwareManager.Escape (cc);
                    //= USBSTOR\DISK&VEN_HTC&PROD_ANDROID_PHONE&REV_0100\8&5901EA1&0&HT851N003062&0
                    var query = String.Format ("SELECT * FROM CIM_USBDevice WHERE DeviceID LIKE '%{0}'", cc);

                    Console.WriteLine ("query = {0}", query);
                    var u = HardwareManager.Query (query).FirstOrDefault ();
                    if (u != null) {
                        usb_device = new UsbDevice (u);
                    }
                }
            }

            // Look up the MountPoint, going from DiskDrive -> Partition -> LogicalDisk
            ManagementObject logical_disk = null;
            foreach (ManagementObject b in o.GetRelated("Win32_DiskPartition")) {
                Console.WriteLine("  PartName: {0}", b["Name"]);
                /*foreach (var p in b.Properties) {
                    Console.WriteLine ("  part prop {0} = {1}", p.Name, p.Value);
                }*/
                foreach (ManagementObject c in b.GetRelated("Win32_LogicalDisk")) {
                    Console.WriteLine("    LDName: {0}", c["Name"]); // here it will print drive letter
                    logical_disk = c;
                }
            }

            Console.WriteLine ("logical disk null? {0}", logical_disk == null);
            if (logical_disk != null) {
                var ld = logical_disk;
                Console.WriteLine ("Got Logical disk!!");
                //IsMounted = (bool)ld.GetPropertyValue ("Automount") == true;
                //IsReadOnly = (ushort) ld.GetPropertyValue ("Access") == 1;
                MountPoint = ld.Str ("Name") + "/";
                FileSystem = ld.Str ("FileSystem");
                Capacity = (ulong) ld.GetPropertyValue ("Size");
                Available = (long)(ulong)ld.GetPropertyValue ("FreeSpace");
            }
        }

        // IVolume

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

        // IDevice

        public IUsbDevice ResolveRootUsbDevice ()
        {
            return usb_device;
        }

        public IUsbPortInfo ResolveUsbPortInfo ()
        {
            return null;
        }

        public string Uuid { get; set; }
        public string Serial { get; set; }
        public string Name { get; set; }
        public string Product { get; set; }
        public string Vendor { get; set; }
        public IDeviceMediaCapabilities MediaCapabilities { get; set; }
    }

    public class HardwareManager : IHardwareManager
    {
        public HardwareManager ()
        {
        }

        public event DeviceAddedHandler DeviceAdded;
        public event DeviceRemovedHandler DeviceRemoved;

        public IEnumerable<IDevice> GetAllDevices ()
        {
            return Query ("SELECT * FROM Win32_DiskDrive").Select (o => new Volume (o) as IDevice);
        }

        private IEnumerable<T> GetAllBlockDevices<T> () where T : IBlockDevice
        {
            yield break;
        }

        public IEnumerable<IBlockDevice> GetAllBlockDevices ()
        {
            yield break;
        }

        public IEnumerable<ICdromDevice> GetAllCdromDevices ()
        {
            yield break;
        }

        public IEnumerable<IDiskDevice> GetAllDiskDevices ()
        {
            yield break;
        }

        public static IEnumerable<ManagementObject> Query (string q)
        {
            using (var s = new ManagementObjectSearcher (new ObjectQuery (q))) {
                foreach (ManagementObject o in s.Get ()) {
                    yield return o;
                }
            }
        }

        public static string Escape (string input)
        {
            input = input.Replace ("_", "[_]"); // escape _'s
            input = input.Replace ("\\", "\\\\"); // escape _'s
            return input;
        }

        public void Dispose ()
        {
        }
    }

    internal static class Extensions
    {
        public static string Str (this ManagementObject o, string propName)
        {
            var p = o.GetPropertyValue (propName);
            return p == null ? null : p.ToString ();
        }
    }
}