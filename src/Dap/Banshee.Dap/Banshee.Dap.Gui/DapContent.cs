//
// DapContent.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.Linq;

using Gtk;

using Hyena;
using Hyena.Data;
using Hyena.Widgets;

using Mono.Unix;

using Banshee.Dap;
using Banshee.Sources.Gui;
using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.Preferences.Gui;

namespace Banshee.Dap.Gui
{
    public class DapContent : DapPropertiesDisplay
    {
        private Label title;
        private DapSource dap;

        // Ugh, this is to avoid the GLib.MissingIntPtrCtorException seen by some; BGO #552169
        protected DapContent (IntPtr ptr) : base (ptr)
        {
        }

        public DapContent (DapSource source) : base (source)
        {
            dap = source;

            BuildWidgets ();
            BuildActions ();
            dap.Properties.PropertyChanged += OnPropertyChanged;
        }

        private void BuildWidgets ()
        {
            HBox split_box = new HBox ();
            VBox content_box = new VBox ();

            content_box.BorderWidth = 5;

            title = new Label ();
            SetTitleText (dap.Name);
            title.Xalign = 0.0f;

            // Define custom preference widgetry
            var hbox = new HBox ();
            var table = new Table ((uint)dap.Sync.Libraries.Count (), 2, false) {
                RowSpacing = 6,
                ColumnSpacing = 6
            };
            uint i = 0;
            foreach (var library in dap.Sync.Libraries) {
                // Translators: {0} is the name of a library, eg 'Music' or 'Podcasts'
                var label = new Label (String.Format (Catalog.GetString ("{0}:"), library.Name)) { Xalign = 1f };
                table.Attach (label, 0, 1, i, i + 1);

                var combo = ComboBox.NewText ();
                combo.RowSeparatorFunc = (model, iter) => { return (string)model.GetValue (iter, 0) == "---"; };
                combo.AppendText (Catalog.GetString ("Manage manually"));
                combo.AppendText (Catalog.GetString ("Sync entire library"));

                var playlists = library.Children.Where (c => c is Banshee.Playlist.AbstractPlaylistSource).ToList ();
                if (playlists.Count > 0) {
                    combo.AppendText ("---");

                    foreach (var playlist in playlists) {
                        // Translators: {0} is the name of a playlist
                        combo.AppendText (String.Format (Catalog.GetString ("Sync from '{0}'"), playlist.Name));
                    }
                }

                combo.Active = 0;
                table.Attach (combo, 1, 2, i, i + 1);
                i++;
            }
            hbox.PackStart (table, false, false, 0);
            hbox.ShowAll ();
            dap.Preferences["sync"]["library-options"].DisplayWidget = hbox;

            Banshee.Preferences.Gui.NotebookPage properties = new Banshee.Preferences.Gui.NotebookPage (dap.Preferences);
            properties.BorderWidth = 0;

            content_box.PackStart (title, false, false, 0);
            content_box.PackStart (properties, false, false, 0);

            Image image = new Image (LargeIcon);
            image.Yalign = 0.0f;

            split_box.PackStart (image, false, true, 0);
            split_box.PackEnd (content_box, true, true, 0);

            Add (split_box);
            ShowAll ();
        }

        private void BuildActions ()
        {
            if (actions == null) {
                actions = new DapActions ();
            }
        }

        private void SetTitleText (string name)
        {
            title.Markup = String.Format ("<span size=\"x-large\" weight=\"bold\">{0}</span>", name);
        }

        private void OnPropertyChanged (object o, PropertyChangeEventArgs args)
        {
            if (args.PropertyName == "UnmapSourceActionLabel")
                SetTitleText (args.NewValue.ToString ());
        }

        private static Banshee.Gui.BansheeActionGroup actions;
    }
}
