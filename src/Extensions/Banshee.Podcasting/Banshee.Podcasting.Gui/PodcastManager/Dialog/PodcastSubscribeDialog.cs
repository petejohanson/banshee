/***************************************************************************
 *  PodcastSubscribeDialog.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
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
using Mono.Unix;

using Hyena.Widgets;

using Migo.Syndication;

using Banshee.Gui;
using Banshee.Base;
using Banshee.Podcasting;
using Banshee.Podcasting.Data;

namespace Banshee.Podcasting.Gui
{
    internal class PodcastSubscribeDialog : Dialog
    {
        private Entry url_entry;
        private Gtk.AccelGroup accelGroup;
        CheckButton download_check, archive_check;

        public string Url {
            get { return url_entry.Text; }
            set { url_entry.Text = value; }
        }

        public FeedAutoDownload SyncPreference {
            get { return download_check.Active ? FeedAutoDownload.All : FeedAutoDownload.None; }
        }

        public int MaxItemCount {
            get { return archive_check.Active ? 1 : 0; }
        }

        public PodcastSubscribeDialog () : base (Catalog.GetString("Subscribe"), null, DialogFlags.Modal | DialogFlags.NoSeparator)
        {
            accelGroup = new Gtk.AccelGroup();
            AddAccelGroup (accelGroup);
            BuildWindow ();
        }

        private void BuildWindow ()
        {
            DefaultWidth = 475;

            BorderWidth = 6;
            VBox.Spacing = 12;
            ActionArea.Layout = Gtk.ButtonBoxStyle.End;

            HBox box = new HBox();
            box.BorderWidth = 6;
            box.Spacing = 12;

            Image image = new Image (IconThemeUtils.LoadIcon (48, "podcast"));

            image.Yalign = 0.0f;

            box.PackStart(image, false, true, 0);

            VBox contentBox = new VBox();
            contentBox.Spacing = 12;

            Label header = new Label();
            header.Markup = String.Format (
                "<big><b>{0}</b></big>",
                GLib.Markup.EscapeText (Catalog.GetString ("Subscribe to New Podcast"))
            );

            header.Justify = Justification.Left;
            header.SetAlignment (0.0f, 0.0f);

            var message = new WrapLabel () {
                Markup = Catalog.GetString (
                    "Please enter the URL of the podcast to which you would like to subscribe."
                ),
                Wrap = true
            };

            url_entry = new Entry ();
            url_entry.ActivatesDefault = true;

            // If the user has copied some text to the clipboard that starts with http, set
            // our url entry to it and select it
            Clipboard clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
            if (clipboard != null) {
                string pasted = clipboard.WaitForText ();
                if (!String.IsNullOrEmpty (pasted)) {
                    if (pasted.StartsWith ("http")) {
                        url_entry.Text = pasted.Trim ();
                        url_entry.SelectRegion (0, url_entry.Text.Length);
                    }
                }
            }

            contentBox.PackStart (header, true, true, 0);
            contentBox.PackStart (message, true, true, 0);

            var url_box = new HBox () { Spacing = 12 };
            url_box.PackStart (new Label (Catalog.GetString ("URL:")), false, false, 0);
            url_box.PackStart (url_entry, true, true, 0);
            contentBox.PackStart (url_box, false, false, 0);

            var options_label = new Label () {
                Markup = String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (Catalog.GetString ("Subscription Options"))),
                Xalign = 0f
            };
            download_check = new CheckButton (Catalog.GetString ("Download new episodes"));
            archive_check = new CheckButton (Catalog.GetString ("Archive all episodes except the newest one"));
            var options_box = new VBox () { Spacing = 3 };
            options_box.PackStart (options_label, false, false, 0);
            options_box.PackStart (new Gtk.Alignment (0f, 0f, 0f, 0f) { LeftPadding = 12, Child = download_check }, false, false, 0);
            options_box.PackStart (new Gtk.Alignment (0f, 0f, 0f, 0f) { LeftPadding = 12, Child = archive_check }, false, false, 0);
            contentBox.PackStart (options_box, false, false, 0);

            box.PackStart (contentBox, true, true, 0);

            AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, true);
            AddButton (Catalog.GetString ("Subscribe"), ResponseType.Ok, true);

            box.ShowAll ();
            VBox.Add (box);
        }

        private void AddButton (string stock_id, Gtk.ResponseType response, bool is_default)
        {
            Gtk.Button button = new Gtk.Button (stock_id);
            button.CanDefault = true;
            button.Show ();

            AddActionWidget (button, response);

            if (is_default) {
                DefaultResponse = response;

                button.AddAccelerator (
                    "activate", accelGroup,
                    (uint) Gdk.Key.Escape, 0, Gtk.AccelFlags.Visible
                );
            }
        }
    }
}
