/***************************************************************************
 *  Tile.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW:
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),
 *  to deal in the Software without restriction, including without limitation
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using Gtk;

namespace Banshee.Widgets
{
    public class Tile : Button
    {
        private const int pixbuf_size = 40;

        private Image image = new Image ();
        public Label PrimaryLabel { get; private set; }
        public Label SecondaryLabel { get; private set; }

        private string primary_text;
        private string secondary_text;

        public Tile (string primaryText) : base ()
        {
            PrimaryText = primaryText;
        }

        public Tile ()
        {
            Table table = new Table (2, 2, false);
            table.ColumnSpacing = 6;
            table.RowSpacing = 2;
            table.BorderWidth = 2;

            PrimaryLabel = new Label ();
            SecondaryLabel = new Label ();

            table.Attach (image, 0, 1, 0, 2, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
            table.Attach (PrimaryLabel, 1, 2, 0, 1,
                AttachOptions.Fill | AttachOptions.Expand,
                AttachOptions.Shrink, 0, 0);
            table.Attach (SecondaryLabel, 1, 2, 1, 2,
                AttachOptions.Fill | AttachOptions.Expand,
                AttachOptions.Fill | AttachOptions.Expand, 0, 0);

            table.ShowAll ();
            Add (table);

            PrimaryLabel.Xalign = 0.0f;
            PrimaryLabel.Yalign = 0.0f;

            SecondaryLabel.Xalign = 0.0f;
            SecondaryLabel.Yalign = 0.0f;

            StyleSet += delegate {
                PrimaryLabel.ModifyFg (StateType.Normal, Style.Text (StateType.Normal));
                SecondaryLabel.ModifyFg (StateType.Normal, Hyena.Gui.GtkUtilities.ColorBlend (
                    Style.Foreground (StateType.Normal), Style.Background (StateType.Normal)));
            };

            Relief = ReliefStyle.None;
        }

        public string PrimaryText {
            get { return primary_text; }
            set {
                primary_text = value;
                PrimaryLabel.Text = value;
            }
        }

        public string SecondaryText {
            get { return secondary_text; }
            set {
                secondary_text = value;
                SecondaryLabel.Markup = String.Format ("<small>{0}</small>",
                    GLib.Markup.EscapeText (value));
            }
        }

        public Gdk.Pixbuf Pixbuf {
            get { return image.Pixbuf; }
            set {
                if(value == null) {
                    return;
                }

                if(value.Width <= pixbuf_size && value.Height <= pixbuf_size) {
                    image.Pixbuf = value;
                    return;
                }

                image.Pixbuf = value.ScaleSimple (pixbuf_size, pixbuf_size,
                    Gdk.InterpType.Bilinear);
            }
        }
    }
}
