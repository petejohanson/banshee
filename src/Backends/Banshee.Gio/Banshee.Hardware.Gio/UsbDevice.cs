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

        internal static UsbDevice ResolveRootDevice (IDevice device)
        {
            // Now walk up the device tree to see if we can find a usb device
            // NOTE: We walk up the tree to find a UDevMetadataSource which
            // exposes the right usb properties, but we never use it. Maybe we
            // should be constructing and wrapping a new RawUsbDevice using the
            // correct metadata. Maybe it doesn't matter. I'm not sure. At the
            // moment we re-use the same RawDevice except wrap it in a UsbDevice.
            IRawDevice raw = device as IRawDevice;
            if (raw != null) {
                var metadata = ResolveUsingUsbBusAndPort (raw.Device.UdevMetadata, true);
                if (metadata == null)
                    metadata = ResolveUsingBusType (raw.Device.UdevMetadata, true);
                if (metadata != null)
                    return new UsbDevice (raw.Device);
            }
            return null;
        }

        public IUsbDevice ResolveRootUsbDevice ()
        {
            return this;
        }

        public IUsbPortInfo ResolveUsbPortInfo ()
        {
            return new UsbPortInfo (BusNumber, DeviceNumber);
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

        public static UsbDevice Resolve (IDevice device)
        {
            IRawDevice raw = device as IRawDevice;
            if (raw != null) {
                if (ResolveUsingUsbBusAndPort (raw.Device.UdevMetadata, false) != null)
                    return new UsbDevice (raw.Device);
                else if (ResolveUsingBusType (raw.Device.UdevMetadata, false) != null)
                    return new UsbDevice (raw.Device);
            }
            return null;
        }

        static UdevMetadataSource ResolveUsingUsbBusAndPort (UdevMetadataSource metadata, bool recurse)
        {
            do {
                if (metadata.PropertyExists (UdevUsbBusNumber) && metadata.PropertyExists (UdevUsbDeviceNumber))
                    return metadata;
            } while (recurse && (metadata = metadata.Parent) != null);
            return null;
        }

        static UdevMetadataSource ResolveUsingBusType (UdevMetadataSource metadata, bool recurse)
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            do {
                if (metadata.PropertyExists ("ID_BUS") && comparer.Equals ("usb", metadata.GetPropertyString ("ID_BUS")))
                    return metadata;
            } while (recurse && (metadata = metadata.Parent) != null);
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
