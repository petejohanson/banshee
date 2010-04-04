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
        private Entry address_entry;
        private SpinButton port_entry;

        public OpenRemoteServer () : base (Catalog.GetString ("Open remote DAAP server"), null)
        {
            VBox.Spacing = 6;
            VBox.PackStart (new Label () {
                Xalign = 0.0f,
                Text = Catalog.GetString ("Enter server name and ip address:")
            }, true, true, 0);

            HBox box = new HBox ();
            box.Spacing = 12;
            VBox.PackStart (box, false, false, 0);

            address_entry = new Entry ();
            address_entry.Activated += OnEntryActivated;
            address_entry.WidthChars = 30;
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
        }

        private void OnEntryActivated (object o, EventArgs args)
        {
            Respond (ResponseType.Ok);
        }

        public string Address {
            get { return address_entry.Text; }
        }

        public int Port {
            get { return port_entry.ValueAsInt; }
        }
    }
}
