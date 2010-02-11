//
// EqualizerSetting.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Alexander Hixon <hixon.alexander@mediati.org>
//
// Copyright 2007-2010 Novell, Inc.
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
using System.Collections.Generic;

using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Configuration;

namespace Banshee.Equalizer
{
    public class EqualizerSetting
    {
        private const uint bandcount = 10;

        private EqualizerManager manager;
        private string name;
        private double [] bands = new double[bandcount];
        private double amp = 0; // amplifier dB (0 dB == passthrough)

        public event EventHandler Changed;

        internal EqualizerSetting (EqualizerManager manager,
            string name, double amp, double [] gains) : this (manager, name)
        {
            IsReadOnly = true;
            for (uint i = 0; i < gains.Length; i++) {
                SetGain (i, gains[i], false);
            }
        }

        public EqualizerSetting (EqualizerManager manager, string name)
        {
            this.manager = manager;
            this.name = name;
        }

        public bool IsReadOnly { get; private set; }

        public string Name {
            get { return name; }
            set {
                name = value;
                OnChanged ();
            }
        }

        public uint BandCount {
            get { return bandcount; }
        }

        public double this[uint band] {
            get { return bands[band]; }
            set { SetGain (band, value, true); }
        }

        public double AmplifierLevel {
            get { return amp; }
            set { SetAmplifierLevel (value, true); }
        }

        public void SetAmplifierLevel (double value, bool flushToEngine)
        {
            amp = value;

            if (!flushToEngine) {
                return;
            }

            if (manager.IsActive) {
                ((IEqualizer)ServiceManager.PlayerEngine.ActiveEngine).AmplifierLevel = value;
            }

            OnChanged ();
        }

        public void SetGain (uint band, double value, bool flushToEngine)
        {
            if (band >= bandcount) {
                throw new ArgumentOutOfRangeException (String.Format (
                    "Band number {0} invalid - only up to {1} bands supported.", band, bandcount));
            }

            bands[band] = value;

            if (!flushToEngine) {
                return;
            }

            if (manager.IsActive) {
                ((IEqualizer)ServiceManager.PlayerEngine.ActiveEngine).SetEqualizerGain (band, value);
            }

            OnChanged ();
        }

        public void FlushToEngine ()
        {
            if (!manager.IsActive) {
                return;
            }

            var engine_eq = (IEqualizer)ServiceManager.PlayerEngine.ActiveEngine;
            engine_eq.AmplifierLevel = AmplifierLevel;
            for (uint band = 0; band < bands.Length; band++) {
                engine_eq.SetEqualizerGain (band, bands[band]);
            }

            OnChanged ();
        }

        protected virtual void OnChanged ()
        {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, new EventArgs ());
            }
        }

        public override string ToString ()
        {
            var builder = new System.Text.StringBuilder ();
            builder.Append ("    {");
            builder.AppendLine ();
            builder.AppendFormat ("        \"name\": \"{0}\",", Name.Replace ("\"", "\\\""));
            builder.AppendLine ();
            builder.AppendFormat ("        \"preamp\": {0},", AmplifierLevel);
            builder.AppendLine ();
            builder.Append ("        \"bands\": [ ");
            for (uint band = 0; band < bands.Length; band++) {
                builder.Append (bands[band]);
                if (band < bands.Length - 1) {
                    builder.Append (", ");
                }
            }
            builder.Append (" ]");
            builder.AppendLine ();
            builder.Append ("    }");
            return builder.ToString ();
        }
    }
}
