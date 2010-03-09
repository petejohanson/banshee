//
// MiniModeWindow.cs
//
// Authors:
//   Aaron Bockover <aaron@abock.org>
//   Felipe Almeida Lessa
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2006 Felipe Almeida Lessa
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
using Gtk;
using Mono.Unix;

using Hyena.Widgets;
using Hyena.Gui;

using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.Gui;
using Banshee.Gui.Widgets;
using Banshee.Sources.Gui;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Widgets;

namespace Banshee.MiniMode
{
    public class MiniMode : Banshee.Gui.BaseClientWindow
    {
        private TrackInfoDisplay track_info_display;
        private ConnectedVolumeButton volume_button;
        private SourceComboBox source_combo_box;
        private ConnectedSeekSlider seek_slider;
        private object tooltip_host;

        private BaseClientWindow default_main_window;

        public MiniMode (BaseClientWindow defaultMainWindow) : base (Catalog.GetString ("Banshee Media Player"), "minimode", 0, 0)
        {
            default_main_window = defaultMainWindow;

            BorderWidth = 12;
            Resizable = false;

            tooltip_host = TooltipSetter.CreateHost ();

            Build ();
            ShowAll ();

            SetHeightLimit ();
        }

        private void Build ()
        {
            var vbox = new VBox () { Spacing = 12 };
            var top = new HBox () { Spacing = 6 };
            var mid = new HBox () { Spacing = 6 };
            var bot = new HBox () { Spacing = 6 };
            vbox.PackStart (top, false, false, 0);
            vbox.PackStart (mid, false, false, 0);
            vbox.PackStart (bot, false, false, 0);

            // Top row: playback buttons, seek slider, full-mode button, volume
            Widget previous_button = ActionService.PlaybackActions["PreviousAction"].CreateToolItem ();
            Widget playpause_button = ActionService.PlaybackActions["PlayPauseAction"].CreateToolItem ();
            Widget button = ActionService.PlaybackActions["NextAction"].CreateToolItem ();
            Menu menu = ActionService.PlaybackActions.ShuffleActions.CreateMenu ();
            MenuButton next_button = new MenuButton (button, menu, true);

            top.PackStart (previous_button, false, false, 0);
            top.PackStart (playpause_button, false, false, 0);
            top.PackStart (next_button, false, false, 0);

            seek_slider = new ConnectedSeekSlider ();
            top.PackStart (seek_slider, true, true, 0);

            var fullmode_button = new Button () {
                Label = Catalog.GetString ("Full Mode"),
                Image = new Image (Stock.LeaveFullscreen, Gtk.IconSize.Button),
                Relief = Gtk.ReliefStyle.None
            };
            fullmode_button.Clicked += OnFullmode;
            top.PackStart (fullmode_button, false, false, 0);

            volume_button = new ConnectedVolumeButton ();
            top.PackStart (volume_button, false, false, 0);

            // Middle row: source dropdown
            source_combo_box = new SourceComboBox ();
            mid.PackStart (source_combo_box, true, true, 0);

            // Bottom row: track info display (cover art, etc), repeat mode button
            track_info_display = new ClassicTrackInfoDisplay ();
            bot.PackStart (track_info_display, true, true, 0);

            var repeat_align = new Alignment (1, 1, 1, 1);
            var repeat_toggle_button = new RepeatActionButton ();
            repeat_align.Add (repeat_toggle_button);
            bot.PackEnd (repeat_align, false, false, 0);

            SetTip (fullmode_button, Catalog.GetString ("Switch back to full mode"));
            SetTip (repeat_toggle_button, Catalog.GetString ("Change repeat playback mode"));

            Add (vbox);
        }

        protected override void Initialize ()
        {
        }

        private void SetTip (Widget widget, string tip)
        {
            TooltipSetter.Set (tooltip_host, widget, tip);
        }

        private void SetHeightLimit ()
        {
            Gdk.Geometry limits = new Gdk.Geometry ();

            limits.MinHeight = -1;
            limits.MaxHeight = -1;
            limits.MinWidth = SizeRequest ().Width;
            limits.MaxWidth = Gdk.Screen.Default.Width;

            SetGeometryHints (this, limits, Gdk.WindowHints.MaxSize | Gdk.WindowHints.MinSize);
        }

        public void Enable ()
        {
            source_combo_box.UpdateActiveSource ();
            default_main_window.Hide ();

            OverrideFullscreen ();

            Show ();
        }

        public void Disable ()
        {
            Hide ();
            RelinquishFullscreen ();
            default_main_window.Show ();
        }

        private void OnFullmode (object o, EventArgs a)
        {
            ElementsService.PrimaryWindow = default_main_window;
            Disable ();
        }

#region Mini-mode Fullscreen Override

        private ViewActions.FullscreenHandler previous_fullscreen_handler;

        private void OverrideFullscreen ()
        {
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> ();
            if (service == null || service.ViewActions == null) {
                return;
            }

            previous_fullscreen_handler = service.ViewActions.Fullscreen;
            service.ViewActions.Fullscreen = FullscreenHandler;
        }

        private void RelinquishFullscreen ()
        {
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> ();
            if (service == null || service.ViewActions == null) {
                return;
            }

            service.ViewActions.Fullscreen = previous_fullscreen_handler;
        }

        private void FullscreenHandler (bool fullscreen)
        {
            // Do nothing, we don't want full-screen while in mini-mode.
        }

#endregion

    }
}
