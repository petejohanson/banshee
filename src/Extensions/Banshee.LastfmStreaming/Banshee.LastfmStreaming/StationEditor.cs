//
// StationEditor.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//   Aaron Bockover <abockover@novell.com>
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
using System.Collections;
using Gtk;
using Mono.Unix;

using Hyena;

using Banshee.Sources;
using Banshee.Database;

using Banshee.Widgets;
using Banshee.Gui.Dialogs;
using Banshee.Lastfm;

namespace Banshee.LastfmStreaming.Radio
{
    public class StationEditor : BansheeDialog
    {
        private LastfmSource lastfm;
        private StationSource source;

        private Gtk.ComboBox type_combo;
        private Gtk.Entry arg_entry;
        private Gtk.Label arg_label;

        public StationEditor (LastfmSource lastfm, StationSource source)
        {
            this.lastfm = lastfm;
            this.source = source;
            Initialize ();
            Title = Catalog.GetString ("Edit Station");
            Arg = source.Arg;
        }

        public StationEditor (LastfmSource lastfm)
        {
            this.lastfm = lastfm;
            Initialize ();
            Title = Catalog.GetString ("New Station");
        }

        private void Initialize ()
        {
            DefaultResponse = Gtk.ResponseType.Ok;
            AddStockButton (Stock.Cancel, ResponseType.Cancel);
            AddStockButton (Stock.Ok, ResponseType.Ok, true);

            SetGeometryHints (this, new Gdk.Geometry () {
                    MinWidth = SizeRequest ().Width,
                    MaxWidth = Gdk.Screen.Default.Width,
                    MinHeight = -1,
                    MaxHeight = -1
                }, Gdk.WindowHints.MaxSize | Gdk.WindowHints.MinSize);

            var table = new Table (2, 2, false) {
                RowSpacing = 12,
                ColumnSpacing = 6
            };

            table.Attach (new Label () {
                    Text = Catalog.GetString ("Station _Type:"),
                    UseUnderline = true,
                    Xalign = 0.0f
                }, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);

            table.Attach (arg_label = new Label () {
                    Xalign = 0.0f
                }, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);

            table.Attach (type_combo = ComboBox.NewText (),
                1, 2, 0, 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);

            table.Attach (arg_entry = new Entry (),
                1, 2, 1, 2, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);

            VBox.PackStart (table, true, true, 0);
            VBox.Spacing = 12;
            VBox.ShowAll ();

            type_combo.RemoveText (0);
            int active_type = 0;
            int i = 0;
            foreach (StationType type in StationType.Types) {
                if (!type.SubscribersOnly || lastfm.Account.Subscriber) {
                    type_combo.AppendText (type.Label);
                    if (source != null && type == source.Type) {
                        active_type = i;
                    }
                    i++;
                }
            }

            type_combo.Changed += HandleTypeChanged;
            type_combo.Active = active_type;
            type_combo.GrabFocus ();
        }

        public void RunDialog ()
        {
            Run ();
            Destroy ();
        }

        protected override void OnResponse (ResponseType response)
        {
            if (response == ResponseType.Ok) {
                string name = SourceName;
                StationType type = StationType.FindByLabel (type_combo.ActiveText);
                string arg = Arg;

                ThreadAssist.Spawn (delegate {
                    if (source == null) {
                        source = new StationSource (lastfm, name, type.Name, arg);
                        lastfm.AddChildSource (source);
                        //LastFMPlugin.Instance.Source.AddChildSource (source);
                        //ServiceManager.SourceManager.AddSource (source);
                    } else {
                        source.Rename (name);
                        source.Type = type;
                        source.Arg = arg;
                        source.Save ();
                        //source.Refresh ();
                    }
                });
            }
        }

        private void HandleTypeChanged (object sender, EventArgs args)
        {
            StationType type = StationType.FindByLabel (type_combo.ActiveText);
            if (type == null)
                Console.WriteLine ("got null type for text: {0}", type_combo.ActiveText);
            else
                arg_label.Text = type.ArgLabel;
        }

        private string SourceName {
            get { return source != null ? source.Name : arg_entry.Text; }
        }

        private string Arg {
            get { return arg_entry.Text; }
            set { arg_entry.Text = value; }
        }
    }
}
