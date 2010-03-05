//
// VideoStreamTile.cs
//
// Author:
//   Kevin Duffus <KevinDuffus@gmail.com>
//
// Copyright (C) 2009 Kevin Duffus
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
using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Web;
using Banshee.Collection;
using Banshee.ServiceStack;

using Hyena;
using Hyena.Widgets;

/*
    Tile layout:
        +===========================+====+
        | +-------+                 |    |
        | |       |   Primary Text  |    |
        | | Image |  Secondary Text | \/ |
        | |       |    * * * * *    |    |
        | +-------+                 |    |
        +===========================+----+
                | Play in Banshee...     |
                | Play in Web Browser... |
                |------------------------|
                | Add to Play Queue...   |
                | Import video...        |
                +------------------------+
*/

namespace Banshee.YouTube.Gui
{
    public class VideoStreamTile : MenuButton
    {
        private Button button = new Button ();
        private Menu menu = new Menu ();
        private ActionGroup action_group;
        private TrackInfo track_info;

        private HBox hbox = new HBox ();
        private VBox vbox = new VBox ();
        private Image image = new Image ();
        private Label primary_label = new Label ();
        private Label secondary_label = new Label ();
        private RatingEntry rating = new RatingEntry ();

        private string video_title;
        private string video_uploader;
        private string primary_text;
        private string secondary_text;

        public VideoStreamTile ()
        {
            hbox.BorderWidth = 2;
            hbox.Spacing = 6;

            vbox.PackStart (primary_label, true, true, 0);
            vbox.PackStart (secondary_label, true, true, 0);
            vbox.PackStart (rating, true, true, 0);

            hbox.PackStart (image, true, true, 0);
            hbox.PackStart (vbox, true, false, 0);

            hbox.ShowAll ();
            button.Add (hbox);
            button.Clicked += new EventHandler (OnButtonClicked);

            primary_label.WidthChars = 24;
            secondary_label.WidthChars = 24;
            primary_label.Ellipsize = Pango.EllipsizeMode.End;
            secondary_label.Ellipsize = Pango.EllipsizeMode.End;

            rating.Sensitive = false;
            rating.HasFrame = false;
            rating.AlwaysShowEmptyStars = true;

            try {
                StyleSet += delegate {
                    primary_label.ModifyFg (StateType.Normal, Style.Text (StateType.Normal));
                    secondary_label.ModifyFg (StateType.Normal, Hyena.Gui.GtkUtilities.ColorBlend (
                        Style.Foreground (StateType.Normal), Style.Background (StateType.Normal)));
                };
            } catch (Exception e) {
                Log.DebugException (e);
            }

            button.Relief = ReliefStyle.None;
            ConstructTile ();
        }

        public string BansheePlaybackUri { get; set; }
        public string BrowserPlaybackUri { get; set; }

        public string Title {
            get { return primary_text; }
            set {
                video_title = value;
                primary_text = video_title;
                primary_label.Text = video_title;
            }
        }

        public string Uploader {
            get { return secondary_text; }
            set {
                video_uploader = value;
                secondary_text = video_uploader;
                secondary_label.Markup = String.Format ("<small>{0} {1}</small>",
                                                        Catalog.GetString ("Uploaded by"),
                                                        GLib.Markup.EscapeText (video_uploader));
            }
        }

        public Gdk.Pixbuf Pixbuf {
            get { return image.Pixbuf; }
            set {
                if (value == null) {
                   return;
                }

                image.Pixbuf = value;
            }
        }

        public int RatingValue {
            get { return rating.Value; }
            set { rating.Value = value; }
        }

        public new Menu Menu {
            get { return menu; }
            set { menu = value; }
        }

        public void AppendMenuItem (Widget menu_item)
        {
            menu.Append (menu_item);
        }

        private Menu CreateMenu ()
        {
            bool separator = false;

            foreach (Gtk.Action action in action_group.ListActions ()) {
                AppendMenuItem (action.CreateMenuItem ());
                if (!separator) {
                    separator = true;
                    AppendMenuItem (new SeparatorMenuItem ());
                }
            }

            menu.ShowAll ();
            return menu;
        }

        private void SetTrackInfo ()
        {
            if (track_info == null) {
                try {
                    track_info = new TrackInfo () {
                        TrackTitle = video_title,
                        ArtistName = video_uploader,
                        AlbumTitle = "YouTube",
                        Uri = new SafeUri (BansheePlaybackUri)
                    };
                } catch (Exception e) {
                    Log.DebugException (e);
                }
            }
        }

        private void OnButtonClicked (object o, EventArgs args)
        {
            if (String.IsNullOrEmpty (BansheePlaybackUri)) {
                Log.Debug ("Banshee supported playback Uri not set");
                return;
            }

            SetTrackInfo ();

            if (track_info != null) {
                ServiceManager.PlayerEngine.OpenPlay (track_info);
            }
        }

        private void OnBansheePlaybackAction (object o, EventArgs args)
        {
            OnButtonClicked (o, args);
        }

        private void OnBrowserPlaybackAction (object o, EventArgs args)
        {
            if (String.IsNullOrEmpty (BrowserPlaybackUri)) {
                Log.Debug ("Browser playback Uri not set");
                return;
            }

            Browser.Open (BrowserPlaybackUri);
        }

        private void ConstructTile ()
        {
            action_group = new ActionGroup ("VideoStreamTileMenuActionGroup");
            action_group.Add (new ActionEntry [] {
                new ActionEntry ("VideoStreamTileBansheePlaybackAction", null,
                    Catalog.GetString ("Play in Banshee..."), null,
                    Catalog.GetString ("Play in Banshee..."), OnBansheePlaybackAction),

                new ActionEntry ("VideoStreamTileBrowserPlaybackAction", null,
                    Catalog.GetString ("Play in Web Browser..."), null,
                    Catalog.GetString ("Play in Web Browser..."), OnBrowserPlaybackAction),
            });

            CreateMenu ();
            Construct (button, menu, true);
        }
    }
}
