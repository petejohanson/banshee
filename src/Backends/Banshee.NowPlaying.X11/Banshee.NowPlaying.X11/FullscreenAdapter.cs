//
// FullScreenAdapter.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2008-2010  Novell, Inc.
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
using System.Runtime.InteropServices;
using Gtk;

using Hyena;
using Banshee.NowPlaying;

namespace Banshee.NowPlaying.X11
{
    public class FullscreenAdapter : IFullscreenAdapter
    {
        private class BaconResize : GLib.InitiallyUnowned
        {
            [DllImport ("libbnpx11")]
            private static extern IntPtr bacon_resize_get_type ();

            public static new GLib.GType GType {
                get { return new GLib.GType (bacon_resize_get_type ()); }
            }

            public BaconResize (Gtk.Window window) : base (IntPtr.Zero)
            {
                this.window = window;

                GLib.Value window_val = new GLib.Value (window);
                CreateNativeObject (
                    new string [] { "video-widget" },
                    new GLib.Value [] { window_val }
                );
                window_val.Dispose ();
            }

            private Window window;
            public Window Window {
                get { return window; }
            }

            [DllImport ("libbnpx11")]
            private static extern void bacon_resize_resize (IntPtr handle);

            public void Resize ()
            {
                bacon_resize_resize (Handle);
            }

            [DllImport ("libbnpx11")]
            private static extern void bacon_resize_restore (IntPtr handle);

            public void Restore ()
            {
                bacon_resize_restore (Handle);
            }

            [GLib.Property ("have-xvidmode")]
            public bool HaveXVidMode {
                get {
                    GLib.Value value = GetProperty ("have-xvidmode");
                    bool ret = (bool)value;
                    value.Dispose ();
                    return ret;
                }
            }
        }

        private BaconResize resize;
        private bool is_fullscreen;

        public event EventHandler SuggestUnfullscreen;

        public FullscreenAdapter ()
        {
            try {
                WnckActiveWorkspaceMonitor.Changed += () => {
                    if (!is_fullscreen) {
                        return;
                    }

                    Hyena.Log.Debug ("WnckScreen::ActiveWorkspaceChanged - raising SuggestUnfullscreen");
                    var handler = SuggestUnfullscreen;
                    if (handler != null) {
                        handler (this, EventArgs.Empty);
                    }
                };
            } catch (Exception e) {
                Hyena.Log.Exception ("Could not configure libwnck support", e);
            }
        }

        public void Fullscreen (Window window, bool fullscreen)
        {
            is_fullscreen = fullscreen;

            // Create the Bacon X11 Resizer if we haven't before or the window changes
            if (resize == null || resize.Window != window) {
                if (resize != null) {
                    resize.Dispose ();
                }

                resize = new BaconResize (window);
                Log.DebugFormat ("X11 Fullscreen Window Set (HaveXVidMode = {0})", resize.HaveXVidMode);
            }

            // Do the default GTK fullscreen operation
            if (fullscreen) {
                window.Fullscreen ();
            } else {
                window.Unfullscreen ();
            }

            // Skip if we don't support xvidmode, otherwise do the good resizing
            if (!resize.HaveXVidMode) {
                return;
            }

            if (fullscreen) {
                resize.Resize ();
            } else {
                resize.Restore ();
            }
        }

        public void Dispose ()
        {
            if (resize != null) {
                resize.Dispose ();
                resize = null;
            }
        }

#region WNCK

        public delegate void WnckActiveWorkspaceChangedHandler (object o, WnckActiveWorkspaceChangedArgs args);

        public class WnckActiveWorkspaceChangedArgs : GLib.SignalArgs
        {
            public IntPtr PreviousWorkspace {
                get { return (IntPtr)Args[0]; }
            }
        }

        private static class WnckActiveWorkspaceMonitor
        {
            private const string WNCK_LIB = "libwnck-1.so.22";
            private const string GOBJECT_LIB = "libgobject-2.0.so";

            [DllImport (WNCK_LIB)]
            private static extern IntPtr wnck_screen_get_default ();

            [DllImport (WNCK_LIB)]
            private static extern void wnck_screen_force_update (IntPtr screen);

            [DllImport (WNCK_LIB)]
            private static extern IntPtr wnck_screen_get_active_workspace (IntPtr screen);

            private delegate void WnckActiveWorkspaceChangedHandler (IntPtr screen,
                IntPtr previouslyActiveWorkspace, IntPtr user);

            [DllImport (GOBJECT_LIB)]
            private static extern ulong g_signal_connect_data (IntPtr instance, string detailed_signal,
                WnckActiveWorkspaceChangedHandler c_handler, IntPtr user, IntPtr notify_closure, uint connect_flags);

            private static WnckActiveWorkspaceChangedHandler active_workspace_changed_handler;

            static WnckActiveWorkspaceMonitor ()
            {
                active_workspace_changed_handler = new WnckActiveWorkspaceChangedHandler (OnActiveWorkspaceChanged);
                var screen = wnck_screen_get_default ();
                wnck_screen_force_update (screen);
                g_signal_connect_data (screen, "active-workspace-changed",
                    active_workspace_changed_handler, IntPtr.Zero, IntPtr.Zero, 0);
            }

            public static event System.Action Changed;

            private static void OnActiveWorkspaceChanged (IntPtr screen, IntPtr previouslyActiveWorkspace, IntPtr user)
            {
                if (wnck_screen_get_active_workspace (screen) == IntPtr.Zero) {
                    Hyena.Log.Debug ("wnck_screen_get_active_workspace returned NULL, " +
                        "wnck_screen_force_update might have failed, " +
                        "so not raising workspace changed event.");
                } else {
                    var handler = Changed;
                    if (handler != null) {
                        handler ();
                    }
                }
            }
        }

#endregion

    }
}
