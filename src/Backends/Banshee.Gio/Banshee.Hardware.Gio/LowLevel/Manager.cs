//
// Manager.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using GLib;
using GUdev;
using System.Runtime.InteropServices;
using Banshee.Hardware;

namespace Banshee.Hardware.Gio
{
    public class Manager : IEnumerable<IDevice>, IDisposable
    {
        private readonly string[] subsystems = new string[] {"block", "usb"};

        private Client client;
        private VolumeMonitor monitor;
        private Dictionary<string, string> device_uuids;

        public event EventHandler<MountArgs> DeviceAdded;
        public event EventHandler<MountArgs> DeviceRemoved;

        public Manager ()
        {
            device_uuids = new Dictionary<string, string> ();
            client = new Client (subsystems);
            monitor = VolumeMonitor.Default;
            monitor.MountAdded += HandleMonitorMountAdded;
            monitor.MountRemoved += HandleMonitorMountRemoved;
            monitor.VolumeRemoved += HandleMonitorVolumeRemoved;
        }

#region IDisposable
        public void Dispose ()
        {
            client.Dispose ();
            monitor.Dispose ();
        }
#endregion

        void HandleMonitorMountAdded (object o, MountAddedArgs args)
        {
            // Manually get the mount as gio-sharp translates it to the wrong managed object
            var mount = GLib.MountAdapter.GetObject ((GLib.Object) args.Args [0]);
            if (mount.Volume == null)
                return;

            var device = GudevDeviceFromGioMount (mount);
            var h = DeviceAdded;
            if (h != null) {
                var v = new RawVolume (mount.Volume,
                                          this,
                                          new GioVolumeMetadataSource (mount.Volume),
                                          new UdevMetadataSource (device));
                h (this, new MountArgs (HardwareManager.Resolve (new Device (v))));
                device_uuids [mount.Volume.Name] = v.Uuid;
            }
        }

        void HandleMonitorMountRemoved (object o, MountRemovedArgs args)
        {
            // Manually get the mount as gio-sharp translates it to the wrong managed object
            var mount = GLib.MountAdapter.GetObject ((GLib.Object) args.Args [0]);
            if (mount.Volume == null || mount.Root == null || mount.Root.Path == null) {
                return;
            }

            VolumeRemoved (mount.Volume);
        }

        void HandleMonitorVolumeRemoved (object o, VolumeRemovedArgs args)
        {
            var volume = GLib.VolumeAdapter.GetObject ((GLib.Object) args.Args [0]);
            if (volume == null) {
                return;
            }

            VolumeRemoved (volume);
        }


        void VolumeRemoved (GLib.Volume volume)
        {
            var h = DeviceRemoved;
            if (h != null) {
                var device = GudevDeviceFromGioVolume (volume);
                var v = new RawVolume (volume,
                                          this,
                                          new GioVolumeMetadataSource (volume),
                                          new UdevMetadataSource (device));

                if (device_uuids.ContainsKey (volume.Name)) {
                    v.Uuid = device_uuids [volume.Name];
                    device_uuids.Remove (volume.Name);
                }

                h (this, new MountArgs (new Device (v)));
            }
        }

        public IEnumerable<IDevice> GetAllDevices ()
        {
            foreach (GLib.Volume vol in monitor.Volumes) {
                var device = GudevDeviceFromGioVolume (vol);
                if (device == null) {
                    continue;
                }

                var raw = new RawVolume (vol,
                                         this,
                                         new GioVolumeMetadataSource (vol),
                                         new UdevMetadataSource (device));
                yield return HardwareManager.Resolve (new Device (raw));
            }
        }

        public GUdev.Device GudevDeviceFromSubsystemPropertyValue (string sub, string prop, string val)
        {
            foreach (GUdev.Device dev in client.QueryBySubsystem (sub)) {
                if (dev.HasProperty (prop) && dev.GetProperty (prop) == val)
                    return dev;
            }

            return null;
        }


        public GUdev.Device GudevDeviceFromGioDrive (GLib.Drive drive)
        {
            GUdev.Device device = null;

            if (drive == null) {
                return null;
            }

            string devFile = drive.GetIdentifier ("unix-device");
            if (!String.IsNullOrEmpty (devFile)) {
                device = client.QueryByDeviceFile (devFile);
            }

            return device;
        }

        public GUdev.Device GudevDeviceFromGioVolume (GLib.Volume volume)
        {
            GUdev.Device device = null;

            if (volume == null) {
                return null;
            }

            var s = volume.GetIdentifier ("unix-device");
            if (!String.IsNullOrEmpty (s)) {
                device = client.QueryByDeviceFile (s);
            }

            if (device == null) {
                s = volume.Uuid;
                foreach (GUdev.Device d in client.QueryBySubsystem ("usb")) {
                    if (s == d.GetSysfsAttr ("serial")) {
                        device = d;
                        break;
                    }
                }
            }

            return device;
        }

        public GUdev.Device GudevDeviceFromGioMount (GLib.Mount mount)
        {
            if (mount == null) {
                return null;
            }

            return GudevDeviceFromGioVolume (mount.Volume);
        }

#region IEnumerable
        public IEnumerator<IDevice> GetEnumerator ()
        {
            foreach (var device in GetAllDevices ())
                yield return device;
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }
#endregion
    }

    public class MountArgs : EventArgs
    {
        public IDevice Device {
            get; private set;
        }

        public MountArgs (IDevice device)
        {
            Device = device;
        }
    }
}
#endif
