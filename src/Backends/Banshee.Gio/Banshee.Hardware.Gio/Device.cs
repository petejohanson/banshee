//
// Device.cs
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
using System.Collections.Generic;
using System.Linq;

using GLib;
using Banshee.Hardware;

namespace Banshee.Hardware.Gio
{
    class Device : IDevice, IRawDevice
    {
        public static IDevice Resolve (IDevice device)
        {
            var raw = device as IRawDevice;
            if (raw != null) {
                return new Device (raw.Device);
            }
            return null;
        }

        protected RawDevice device;
        RawDevice IRawDevice.Device {
            get { return device; }
        }

        public IDeviceMediaCapabilities MediaCapabilities {
            get { return device.MediaCapabilities; }
        }

        public string Name {
            get { return device.Name; }
        }

        public string Serial {
            get { return device.Serial; }
        }

        public string Product {
            get { return device.Product; }
        }

        public string Uuid {
            get { return device.Uuid; }
        }

        public string Vendor {
            get { return device.Vendor; }
        }

        public Device (RawDevice device)
        {
            this.device = device;
        }

        public IUsbDevice ResolveRootUsbDevice ()
        {
            return UsbDevice.ResolveRootDevice (this);
        }

        public IUsbPortInfo ResolveUsbPortInfo ()
        {
            var f = UsbDevice.ResolveRootDevice (this);
            if (f != null) {
                return new UsbPortInfo (f.BusNumber, f.DeviceNumber);
            }
            return null;
        }

        public bool PropertyExists (string key)
        {
            return device.PropertyExists (key);
        }

        public string GetPropertyString (string key)
        {
            return device.GetPropertyString (key);
        }

        public double GetPropertyDouble (string key)
        {
            return device.GetPropertyDouble (key);
        }

        public bool GetPropertyBoolean (string key)
        {
            return device.GetPropertyBoolean (key);
        }

        public int GetPropertyInteger (string key)
        {
            return device.GetPropertyInteger (key);
        }

        public ulong GetPropertyUInt64 (string key)
        {
            return device.GetPropertyUInt64 (key);
        }

        public string[] GetPropertyStringList (string key)
        {
            return device.GetPropertyStringList (key);
        }
    }
}
#endif
