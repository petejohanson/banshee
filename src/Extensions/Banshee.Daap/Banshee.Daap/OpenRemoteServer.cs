//
// OpenRemoteServer.cs
//
// Author:
//   Félix Velasco <felix.velasco@gmail.com>
//
// Copyright (C) 2009 Félix Velasco
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
using Banshee.Gui.Dialogs;
using Banshee.Configuration;

namespace Banshee.Daap
{
    public class OpenRemoteServer : BansheeDialog
    {
        private ComboBoxEntry address_entry;
        private SpinButton port_entry;
        private List<string> history = new List<string>();

        public OpenRemoteServer () : base (Catalog.GetString ("Open remote DAAP server"), null)
        {
            VBox.Spacing = 6;
            VBox.PackStart (new Label () {
                Xalign = 0.0f,
                Text = Catalog.GetString ("Enter server IP address and port:")
            }, true, true, 0);

            HBox box = new HBox ();
            box.Spacing = 12;
            VBox.PackStart (box, false, false, 0);

            address_entry = ComboBoxEntry.NewText ();
            address_entry.Entry.Activated += OnEntryActivated;
            address_entry.Entry.WidthChars = 30;
            address_entry.Show ();

            port_entry = new SpinButton (1d, 65535d, 1d);
            port_entry.Value = 3689;
            port_entry.Show ();

            box.PackStart (address_entry, true, true, 0);
            box.PackEnd (port_entry, false, false, 0);

            address_entry.HasFocus = true;

            VBox.ShowAll ();

            AddStockButton (Stock.Cancel, ResponseType.Cancel);
            AddStockButton (Stock.Ok, ResponseType.Ok, true);

            LoadHistory();
        }

        protected override void OnResponse (ResponseType responseId)
        {
            if (responseId != ResponseType.Ok) {
                return;
            }

            var filtered_history = new List<string> ();

            history.Insert (0, Address);
            foreach (string uri in history) {
                if (!filtered_history.Contains (uri)) {
                    filtered_history.Add (uri);
                }
            }

            string [] trimmed_history = new string [Math.Min (15, filtered_history.Count)];
            for (int i = 0; i < trimmed_history.Length; i++) {
                trimmed_history[i] = filtered_history[i];
            }

            OpenRemoteServerHistorySchema.Set (trimmed_history);
        }

        private void OnEntryActivated (object o, EventArgs args)
        {
            Respond (ResponseType.Ok);
        }

        public string Address {
            get { return address_entry.Entry.Text; }
        }

        public int Port {
            get { return port_entry.ValueAsInt; }
        }

        private void LoadHistory()
        {
            string [] history_array = OpenRemoteServerHistorySchema.Get ();
            if (history_array == null || history_array.Length == 0) {
                return;
            }

            foreach (string uri in history_array) {
                history.Add (uri);
                address_entry.AppendText (uri);
            }
        }

        public static readonly SchemaEntry<string []> OpenRemoteServerHistorySchema = new SchemaEntry<string []>(
            "plugins.daap", "open_remote_server_history",
            new string [] { String.Empty },
            "URI List",
            "List of URIs in the history drop-down for the open remote server dialog"
        );
    }
}
