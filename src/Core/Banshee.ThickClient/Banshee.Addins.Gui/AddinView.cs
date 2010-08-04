//
// AddinView.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.Collections.Generic;
using Gtk;

using Hyena;

using Mono.Unix;
using Mono.Addins;

namespace Banshee.Addins.Gui
{
    public class AddinView : VBox
    {
        private TreeView tree_view;

        public AddinView ()
        {
            var hbox = new HBox () { Spacing = 6 };

            var filter_label = new Label (Catalog.GetString ("Show:"));
            var filter_combo = ComboBox.NewText ();
            filter_combo.AppendText (Catalog.GetString ("All"));
            filter_combo.AppendText (Catalog.GetString ("Enabled"));
            filter_combo.AppendText (Catalog.GetString ("Not Enabled"));
            filter_combo.Active = 0;

            var search_label = new Label (Catalog.GetString ("Search:"));
            var search_entry = new Banshee.Widgets.SearchEntry () {
                WidthRequest = 160,
                Visible = true,
                Ready = true
            };

            hbox.PackStart (filter_label, false, false, 0);
            hbox.PackStart (filter_combo, false, false, 0);
            hbox.PackEnd   (search_entry, false, false, 0);
            hbox.PackEnd   (search_label, false, false, 0);

            var model = new TreeStore (typeof(bool), typeof(bool), typeof (string), typeof (Addin));

            var addins = AddinManager.Registry.GetAddins ().Where (a => { return
                a.Name != a.Id && a.Description != null &&
                !String.IsNullOrEmpty (a.Description.Category) && !a.Description.Category.StartsWith ("required:") &&
                (!a.Description.Category.Contains ("Debug") || ApplicationContext.Debugging);
            });

            var categorized_addins = addins.GroupBy<Addin, string> (a => a.Description.Category)
                                           .Select (c => new {
                                                Addins = c.OrderBy (a => Catalog.GetString (a.Name)).ToList (),
                                                Name = c.Key,
                                                NameLocalized = Catalog.GetString (c.Key) })
                                           .OrderBy (c => c.NameLocalized)
                                           .ToList ();

            tree_view = new TreeView () {
                FixedHeightMode = false,
                HeadersVisible = false,
                SearchColumn = 1,
                RulesHint = true,
                Model = model
            };

            var update_model = new System.Action (() => {
                string search = search_entry.Query;
                bool? enabled = filter_combo.Active > 0 ? (bool?) (filter_combo.Active == 1 ? true : false) : null;
                model.Clear ();
                foreach (var cat in categorized_addins) {
                    var cat_iter = model.AppendValues (false, false, String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (cat.NameLocalized)), null);
                    bool any = false;
                    foreach (var a in cat.Addins.Matching (search)) {
                        if (enabled == null || (a.Enabled == enabled.Value)) {
                            model.AppendValues (cat_iter, true,
                                a.Enabled,
                                String.Format (
                                    "<b>{0}</b>\n<small>{1}</small>",
                                    GLib.Markup.EscapeText (Catalog.GetString (a.Name)),
                                    GLib.Markup.EscapeText (Catalog.GetString (a.Description.Description))),
                                a
                            );
                            any = true;
                        }
                    }

                    if (!any) {
                        model.Remove (ref cat_iter);
                    }
                }
                tree_view.ExpandAll ();
            });

            var txt_cell = new CellRendererText () { WrapMode = Pango.WrapMode.Word };
            tree_view.AppendColumn ("Name", txt_cell , "markup", Columns.Name);

            var check_cell = new CellRendererToggle () { Activatable = true };
            tree_view.AppendColumn ("Enable", check_cell, "visible", Columns.IsAddin, "active", Columns.IsEnabled);
            check_cell.Toggled += (o, a) => {
                TreeIter iter;
                if (model.GetIter (out iter, new TreePath (a.Path))) {
                    var addin = model.GetValue (iter, 3) as Addin;
                    bool enabled = (bool) model.GetValue (iter, 1);
                    addin.Enabled = !enabled;
                    model.SetValue (iter, 1, addin.Enabled);
                    model.Foreach (delegate (TreeModel current_model, TreePath path, TreeIter current_iter) {
                        var an = current_model.GetValue (current_iter, 3) as Addin;
                        if (an != null) {
                            current_model.SetValue (current_iter, 1, an.Enabled);
                        }
                        return false;
                    });
                }
            };

            update_model ();
            search_entry.Changed += (o, a) => update_model ();
            filter_combo.Changed += (o, a) => update_model ();

            var tree_scroll = new Hyena.Widgets.ScrolledWindow () {
                HscrollbarPolicy = PolicyType.Never
            };
            tree_scroll.AddWithFrame (tree_view);

            Spacing = 6;
            PackStart (hbox, false, false, 0);
            PackStart (tree_scroll, true, true, 0);
            ShowAll ();
            search_entry.InnerEntry.GrabFocus ();

            txt_cell.WrapWidth = 300;
        }

        private enum Columns : int {
            IsAddin,
            IsEnabled,
            Name,
            Addin
        };
    }

    internal static class AddinExtensions
    {
        public static IEnumerable<Addin> Matching (this IEnumerable<Addin> addins, string search)
        {
            search = StringUtil.SearchKey (search);
            if (String.IsNullOrEmpty (search)) {
                return addins;
            }

            return addins.Where (a => a.MatchStrings ().Any (s => {
                return StringUtil.SearchKey (s).Contains (search) ||
                       StringUtil.SearchKey (Catalog.GetString (s)).Contains (search);
            }));
        }

        public static IEnumerable<string> MatchStrings (this Addin a)
        {
            yield return a.Name;
            yield return a.Description.Description;
            yield return a.Description.Category;
        }
    }
}
