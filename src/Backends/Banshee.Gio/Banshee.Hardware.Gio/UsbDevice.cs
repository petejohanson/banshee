//
// UsbDevice.cs
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

using GUdev;
using System.Globalization;

namespace Banshee.Hardware.Gio
{
    class UsbDevice : IUsbDevice, IRawDevice
    {
        const string UdevUsbBusNumber = "BUSNUM";
        const string UdevUsbDeviceNumber = "DEVNUM";
        const string UdevVendorId = "ID_VENDOR_ID";
        const string UdevProductId = "ID_MODEL_ID";

        internal static IUsbDevice ResolveRootDevice (IDevice device)
        {
            // First check if the supplied device is an IUsbDevice itself
            var result = Resolve (device);
            if (result != null) {
                return result;
            }

            // Now walk up the device tree to see if we can find one.
            IRawDevice raw = device as IRawDevice;
            if (raw != null) {
                var parent = raw.Device.UdevMetadata.Parent;
                while (parent != null) {
                    if (parent.PropertyExists (UdevUsbBusNumber) && parent.PropertyExists (UdevUsbDeviceNumber)) {
                        return new UsbDevice (new RawUsbDevice (raw.Device.Manager, raw.Device.GioMetadata, parent));
                    }
                    parent = parent.Parent;
                }
            }
            return null;
        }

        public IUsbDevice ResolveRootUsbDevice ()
        {
            return this;
        }

        public static int GetBusNumber (IUsbDevice device)
        {
            var raw = device as IRawDevice;
            return raw == null ? 0 : int.Parse (raw.Device.UdevMetadata.GetPropertyString (UdevUsbBusNumber));
        }

        public static int GetDeviceNumber (IUsbDevice device)
        {
            var raw = device as IRawDevice;
            return raw ==  null ? 0 : int.Parse (raw.Device.UdevMetadata.GetPropertyString (UdevUsbDeviceNumber));
        }

        public static int GetProductId (IUsbDevice device)
        {
            var raw = device as IRawDevice;
            return raw == null ? 0 : int.Parse (raw.Device.UdevMetadata.GetPropertyString (UdevProductId), NumberStyles.HexNumber);
        }

        public static int GetSpeed (IUsbDevice device)
        {
            throw new NotImplementedException ();
        }

        public static int GetVendorId (IUsbDevice device)
        {
            var raw = device as IRawDevice;
            return raw == null ? 0 : int.Parse (raw.Device.UdevMetadata.GetPropertyString (UdevVendorId), NumberStyles.HexNumber);
        }

        public static int GetVersion (IUsbDevice device)
        {
            throw new NotImplementedException ();
        }

        public static IUsbDevice Resolve (IDevice device)
        {
            IRawDevice raw = device as IRawDevice;
            if (raw != null) {
                var metadata = raw.Device.UdevMetadata;
                if (metadata.PropertyExists (UdevUsbBusNumber) && metadata.PropertyExists (UdevUsbDeviceNumber))
                    return new UsbDevice (raw.Device);
            }
            return null;
        }

        public RawDevice Device {
            get; set;
        }

        public int BusNumber {
            get { return GetBusNumber (this); }
        }

        public int DeviceNumber {
            get { return GetDeviceNumber (this); }
        }

        public string Name {
            get { return Device.Name; }
        }

        public IDeviceMediaCapabilities MediaCapabilities {
            get { return Device.MediaCapabilities; }
        }

        public string Product {
            get { return Device.Product;}
        }

        public int ProductId {
            get { return GetProductId (this); }
        }

        public string Serial {
            get { return Device.Serial; }
        }

        // What is this and why do we want it?
        public double Speed {
            get { return GetSpeed (this); }
        }

        public string Uuid {
            get { return Device.Uuid; }
        }

        public string Vendor {
            get { return Device.Vendor; }
        }

        public int VendorId {
            get { return GetVendorId (this); }
        }

        // What is this and why do we want it?
        public double Version {
            get { return GetVersion (this); }
        }

        UsbDevice (RawDevice device)
        {
            Device = device;
        }

        bool IDevice.PropertyExists (string key)
        {
            return Device.PropertyExists (key);
        }

        string IDevice.GetPropertyString (string key)
        {
            return Device.GetPropertyString (key);
        }

        double IDevice.GetPropertyDouble (string key)
        {
            return Device.GetPropertyDouble (key);
        }

        bool IDevice.GetPropertyBoolean (string key)
        {
            return Device.GetPropertyBoolean (key);
        }

        int IDevice.GetPropertyInteger (string key)
        {
            return Device.GetPropertyInteger (key);
        }

        ulong IDevice.GetPropertyUInt64 (string key)
        {
            return Device.GetPropertyUInt64 (key);
        }

        public string[] GetPropertyStringList (string key)
        {
            return Device.GetPropertyStringList (key);
        }
    }
}
#endif
