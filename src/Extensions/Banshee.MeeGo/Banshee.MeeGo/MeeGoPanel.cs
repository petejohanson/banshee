//
// MeeGoPanel.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Novell, Inc.
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

using Gtk;
using MeeGo.Panel;

using Hyena;
using Banshee.Base;

namespace Banshee.MeeGo
{
    public class MeeGoPanel : IDisposable
    {
        public static MeeGoPanel Instance { get; private set; }

        public PanelGtk ToolbarPanel { get; private set; }
        public uint ToolbarPanelWidth { get; private set; }
        public uint ToolbarPanelHeight { get; private set; }
        public MediaPanelContents Contents { get; private set; }
        public bool BansheeIsInitialized { get; set; }

        public MeeGoPanel ()
        {
            if (Instance != null) {
                throw new ApplicationException ("Only one MeeGoPanel instance can exist");
            }

            if (ApplicationContext.CommandLine.Contains ("mutter-panel")) {
                BuildPanel ();
                Instance = this;
            }
        }

        public void Dispose ()
        {
        }

        private void BuildPanel ()
        {
            try {
                ToolbarPanel = new PanelGtk ("banshee", "media", null, "media-button", true);
                ToolbarPanel.ReadyEvent += (o, e) => {
                    lock (this) {
                        Contents = new MediaPanelContents ();
                        Contents.ShowAll ();
                        ToolbarPanel.SetChild (Contents);
                        if (BansheeIsInitialized) {
                            Contents.BuildViews ();
                        }
                    }
                };
            } catch (Exception e) {
                Log.Exception ("Could not bind to MeeGo panel", e);
                var window = new Gtk.Window ("MeeGo Media Panel");
                window.SetDefaultSize (1000, 500);
                window.WindowPosition = Gtk.WindowPosition.Center;
                window.Add (Contents = new MediaPanelContents ());
                window.ShowAll ();
                GLib.Timeout.Add (1000, () => {
                    window.Present ();
                    return false;
                });
            }
        }
    }
}
