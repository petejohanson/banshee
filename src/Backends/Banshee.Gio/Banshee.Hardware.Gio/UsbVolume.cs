//
// UsbVolume.cs
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
    class UsbVolume : Volume, IUsbDevice
    {
        public static new IDevice Resolve (IDevice device)
        {
            var raw = device as IRawDevice;
            if (raw != null) {
                if (UsbDevice.Resolve (device) != null && Volume.Resolve (device) != null)
                    return new UsbVolume ((RawVolume) raw.Device);
            }
            return null;
        }

        public int BusNumber {
            get { return UsbDevice.GetBusNumber (this); }
        }

        public int DeviceNumber {
            get { return UsbDevice.GetDeviceNumber (this); }
        }

        public int ProductId {
            get {return UsbDevice.GetProductId (this); }
        }

        public double Speed {
            get { return UsbDevice.GetSpeed (this); }
        }

        public int VendorId {
            get { return UsbDevice.GetVendorId (this); }
        }

        public double Version {
            get { return UsbDevice.GetVersion(this); }
        }

        public UsbVolume (RawVolume volume)
            : base (volume)
        {

        }
    }
}

#endif
