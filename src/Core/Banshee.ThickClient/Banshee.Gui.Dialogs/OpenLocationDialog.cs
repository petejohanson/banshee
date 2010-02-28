//
// OpenLocationDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2006-2010 Novell, Inc.
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
using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Configuration;

namespace Banshee.Gui.Dialogs
{
    public class OpenLocationDialog : BansheeDialog
    {
        private ComboBoxEntry address_entry;

        private List<string> history = new List<string>();

        public OpenLocationDialog () : base (Catalog.GetString ("Open Location"))
        {
            var location_box = new HBox () {
                Spacing = 6
            };

            address_entry = ComboBoxEntry.NewText();
            address_entry.Entry.Activated += (o, e) => Respond (ResponseType.Ok);

            var browse_button = new Button(Catalog.GetString("Browse..."));
            browse_button.Clicked += OnBrowseClicked;

            location_box.PackStart(address_entry, true, true, 0);
            location_box.PackStart(browse_button, false, false, 0);

            VBox.Spacing = 6;
            VBox.PackStart (new Label () {
                Xalign = 0.0f,
                Text = Catalog.GetString (
                    "Enter the address of the file you would like to open:")
                }, false, false, 0);
            VBox.PackStart (location_box, false, false, 0);
            VBox.ShowAll ();

            AddStockButton (Stock.Cancel, ResponseType.Cancel);
            AddStockButton (Stock.Open, ResponseType.Ok, true);

            LoadHistory();

            address_entry.Entry.HasFocus = true;
        }

        protected override void OnResponse (ResponseType responseId)
        {
            if (responseId != ResponseType.Ok) {
                return;
            }

            List<string> filtered_history = new List<string>();

            history.Insert(0, Address);
            foreach(string uri in history) {
                if(!filtered_history.Contains(uri)) {
                    filtered_history.Add(uri);
                }
            }

            string [] trimmed_history = new string[Math.Min(15, filtered_history.Count)];
            for(int i = 0; i < trimmed_history.Length; i++) {
                trimmed_history[i] = filtered_history[i] as string;
            }

            OpenLocationHistorySchema.Set(trimmed_history);
        }

        private void OnBrowseClicked(object o, EventArgs args)
        {
            FileChooserDialog chooser = new FileChooserDialog(
                Catalog.GetString("Open Location"),
                null,
                FileChooserAction.Open
            );

            chooser.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            chooser.AddButton(Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton(Stock.Open, ResponseType.Ok);
            chooser.DefaultResponse = ResponseType.Ok;
            chooser.LocalOnly = false;

            if(chooser.Run() == (int)ResponseType.Ok) {
                address_entry.Entry.Text = chooser.Uri;
            }

            chooser.Destroy();
        }

        private void LoadHistory()
        {
            string [] history_array = OpenLocationHistorySchema.Get();
            if(history_array == null || history_array.Length == 0) {
                return;
            }

            foreach(string uri in history_array) {
                history.Add(uri);
                address_entry.AppendText(uri);
            }
        }

        public string Address {
            get { return address_entry.Entry.Text; }
        }

        public static readonly SchemaEntry<string []> OpenLocationHistorySchema = new SchemaEntry<string []>(
            "player_window", "open_location_history",
            new string [] { String.Empty },
            "URI List",
            "List of URIs in the history drop-down for the open location dialog"
        );
    }
}
