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
    /// <summary>
    /// A Device is a wrapper around the two metadata source, udev and gio. Banshee needs information
    /// from both sources, so this Device class is meant to provide a level of abstraction.
    /// </summary>
    abstract class RawDevice : IEquatable<RawDevice>, IComparable<RawDevice>, IRawDevice
    {
        const string UdevDevicePath = "DEVNAME";

        RawDevice IRawDevice.Device {
            get { return this; }
        }

        public string DeviceNode {
            get { return UdevMetadata.GetPropertyString (UdevDevicePath); }
        }

        internal GioMetadataSource GioMetadata {
            get; private set;
        }

        internal UdevMetadataSource UdevMetadata {
            get; private set;
        }

        public abstract string Identifier {
            get;
        }

        public abstract string IdMediaPlayer {
            get;
        }

        public abstract bool IsRemovable {
            get;
        }

        public IDeviceMediaCapabilities MediaCapabilities {
            get; private set;
        }

        public abstract string Name {
            get;
        }

        public Manager Manager {
            get; private set;
        }

        public abstract string Model {
            get;
        }

        public abstract string Product {
            get;
        }

        public abstract string Serial {
            get;
        }

        public abstract string Subsystem {
            get;
        }

        public abstract string Uuid {
            get;
        }

        public abstract string Vendor {
            get;
        }

        protected RawDevice (Manager manager, GioMetadataSource gioMetadata, UdevMetadataSource udevMetadata)
        {
            Manager = manager;
            GioMetadata = gioMetadata;
            UdevMetadata = udevMetadata;
            if (!string.IsNullOrEmpty (IdMediaPlayer))
                MediaCapabilities = new DeviceMediaCapabilities (IdMediaPlayer);
        }

        public bool Equals (RawDevice other)
        {
            return Identifier == other.Identifier;
        }

        public int CompareTo (RawDevice other)
        {
            return string.Compare (Identifier, other.Identifier);
        }

        public override int GetHashCode ()
        {
            return Identifier.GetHashCode ();
        }

        public abstract string GetPropertyString (string key);

        public abstract double GetPropertyDouble (string key);

        public abstract bool GetPropertyBoolean (string key);

        public abstract int GetPropertyInteger (string key);

        public abstract ulong GetPropertyUInt64 (string key);

        public abstract string[] GetPropertyStringList (string key);

        public abstract bool PropertyExists (string key);
    }
}
#endif
