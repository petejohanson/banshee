//
// ImportDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
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
using Gtk;

using Hyena;

using Mono.Unix;

using Banshee.Configuration.Schema;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Gui;
using Banshee.Gui.Dialogs;

namespace Banshee.Library.Gui
{
    public class ImportDialog : BansheeDialog
    {
        private ComboBox source_combo_box;
        private ListStore source_model;
        private Button import_button;
        private CheckButton do_not_show_check_button;

        public ImportDialog () : this (false)
        {
        }

        public ImportDialog (bool doNotShowAgainVisible) : base (String.Empty)
        {
            Resizable = false;
            DefaultResponse = ResponseType.Ok;

            AddStockButton (Stock.Cancel, ResponseType.Cancel);
            import_button = AddButton (String.Empty, ResponseType.Ok, true);

            uint row = 0;

            var table = new Table (doNotShowAgainVisible ? (uint)4 : (uint)3, 2, false) {
                RowSpacing = 12,
                ColumnSpacing = 16,
                Homogeneous = false
            };

            table.Attach (new Label () {
                    Markup = Catalog.GetString ("<big><b>Import Media to Library</b></big>"),
                    Xalign = 0.0f
                }, 1, 2, row, ++row);

            if (ServiceManager.SourceManager.DefaultSource.Count == 0) {
                table.Attach (new Label () {
                        Text = Catalog.GetString ("Your media library is empty. You may import new music and videos into your library now, or choose to do so later."),
                        Xalign = 0.0f,
                        Wrap = true
                    }, 1, 2, row, ++row);
            }

            PopulateSourceList ();

            var vbox = new VBox () { Spacing = 2 };
            vbox.PackStart (new Label () {
                    Text = Catalog.GetString ("Import _from:"),
                    Xalign = 0.0f,
                    UseUnderline = true,
                    MnemonicWidget = source_combo_box
                }, false, false, 0);
            vbox.PackStart (source_combo_box, false, false, 0);
            table.Attach (vbox, 1, 2, row, ++row);

            if (doNotShowAgainVisible) {
                table.Attach (do_not_show_check_button = new CheckButton (
                    Catalog.GetString ("Do not show this dialog again")),
                    1, 2, row, ++row);
            }

            table.Attach (new Image () {
                    IconName = "drive-harddisk",
                    IconSize = (int)IconSize.Dialog,
                    Yalign = 0.0f
                }, 0, 1, 0, row, AttachOptions.Shrink, AttachOptions.Expand | AttachOptions.Fill, 0, 0);

            VBox.PackStart (table, true, true, 0);
            VBox.ShowAll ();

            if (doNotShowAgainVisible) {
                DoNotShowAgainVisible = doNotShowAgainVisible;
            }

            ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            ServiceManager.SourceManager.SourceRemoved += OnSourceRemoved;
            ServiceManager.SourceManager.SourceUpdated += OnSourceUpdated;
        }

        protected override void OnStyleSet (Style previous_style)
        {
            base.OnStyleSet (previous_style);
            UpdateIcons ();
        }

        private void UpdateImportLabel ()
        {
            string label = ActiveSource == null ? null : ActiveSource.ImportLabel;
            import_button.Label = label ?? Catalog.GetString ("_Import");
            import_button.WidthRequest = Math.Max (import_button.WidthRequest, 140);
        }

        private void PopulateSourceList ()
        {
            source_model = new ListStore (typeof (Gdk.Pixbuf), typeof (string), typeof (IImportSource));

            source_combo_box = new ComboBox ();
            source_combo_box.Changed += delegate { UpdateImportLabel (); };
            source_combo_box.Model = source_model;
            source_combo_box.RowSeparatorFunc = (model, iter) => model.GetValue (iter, 2) == null;

            CellRendererPixbuf pixbuf_cr = new CellRendererPixbuf ();
            CellRendererText text_cr = new CellRendererText ();

            source_combo_box.PackStart (pixbuf_cr, false);
            source_combo_box.PackStart (text_cr, true);
            source_combo_box.SetAttributes (pixbuf_cr, "pixbuf", 0);
            source_combo_box.SetAttributes (text_cr, "text", 1);

            TreeIter active_iter = TreeIter.Zero;

            List<IImportSource> sources = new List<IImportSource> ();

            // Add the standalone import sources
            foreach (IImportSource source in ServiceManager.Get<ImportSourceManager> ()) {
                sources.Add (source);
            }

            // Find active sources that implement IImportSource
            foreach (Source source in ServiceManager.SourceManager.Sources) {
                if (source is IImportSource) {
                    sources.Add ((IImportSource)source);
                }
            }

            // Sort the sources by their SortOrder properties
            sources.Sort (import_source_comparer);

            // And actually add them to the dialog
            int? last_sort_order = null;
            foreach (IImportSource source in sources) {
                if (last_sort_order != null && last_sort_order / 10 != source.SortOrder / 10) {
                    source_model.AppendValues (null, null, null);
                }

                AddSource (source);
                last_sort_order = source.SortOrder;
            }

            if (!active_iter.Equals(TreeIter.Zero) || (active_iter.Equals (TreeIter.Zero) &&
                source_model.GetIterFirst (out active_iter))) {
                source_combo_box.SetActiveIter (active_iter);
            }
        }

        private void UpdateIcons ()
        {
            for (int i = 0, n = source_model.IterNChildren (); i < n; i++) {
                TreeIter iter;
                if (source_model.IterNthChild (out iter, i)) {
                    object o = source_model.GetValue (iter, 0);
                    IImportSource source = (IImportSource)source_model.GetValue (iter, 2);
                    if (o != null) {
                        ((Gdk.Pixbuf)o).Dispose ();
                    }

                    if (source != null) {
                        var icon = GetIcon (source);
                        if (icon != null) {
                            source_model.SetValue (iter, 0, icon);
                        }
                    }
                }
            }
        }

        private Gdk.Pixbuf GetIcon (IImportSource source)
        {
            return IconThemeUtils.LoadIcon (22, source.IconNames);
        }

        private TreeIter AddSource (IImportSource source)
        {
            if (source == null) {
                return TreeIter.Zero;
            }

            return source_model.AppendValues (GetIcon (source), source.Name, source);
        }

        private void OnSourceAdded (SourceAddedArgs args)
        {
            if(args.Source is IImportSource) {
                ThreadAssist.ProxyToMain (delegate {
                    AddSource ((IImportSource)args.Source);
                });
            }
        }

        private void OnSourceRemoved (SourceEventArgs args)
        {
            if (args.Source is IImportSource) {
                ThreadAssist.ProxyToMain (delegate {
                    TreeIter iter;
                    if (FindSourceIter (out iter, (IImportSource)args.Source)) {
                        source_model.Remove (ref iter);
                    }
                });
            }
        }

        private void OnSourceUpdated (SourceEventArgs args)
        {
            if (args.Source is IImportSource) {
                ThreadAssist.ProxyToMain (delegate {
                    TreeIter iter;
                    if(FindSourceIter (out iter, (IImportSource)args.Source)) {
                        source_model.SetValue (iter, 1, args.Source.Name);
                    }
                });
            }
        }

        private bool FindSourceIter (out TreeIter iter, IImportSource source)
        {
            iter = TreeIter.Zero;

            for (int i = 0, n = source_model.IterNChildren (); i < n; i++) {
                TreeIter _iter;
                if (source_model.IterNthChild (out _iter, i)) {
                    if (source == source_model.GetValue (_iter, 2)) {
                        iter = _iter;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool DoNotShowAgainVisible {
            get { return do_not_show_check_button.Visible; }
            set { do_not_show_check_button.Visible = value; }
        }

        /*private bool DoNotShowAgain {
            get { return do_not_show_check_button.Active; }
        }*/

        public IImportSource ActiveSource {
            get {
                TreeIter iter;
                if (source_combo_box.GetActiveIter (out iter)) {
                    return (IImportSource)source_model.GetValue (iter, 2);
                }

                return null;
            }
        }

        private static IComparer<IImportSource> import_source_comparer = new ImportSourceComparer ();
        private class ImportSourceComparer : IComparer<IImportSource>
        {
            public int Compare (IImportSource a, IImportSource b)
            {
                int ret = a.SortOrder.CompareTo (b.SortOrder);
                return ret != 0
                    ? ret
                    : a.Name.CompareTo (b.Name);
            }
        }
    }
}
