//
// PersistentColumnController.cs
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
using System.Collections.Generic;

using Hyena.Data;
using Hyena.Data.Gui;
using Banshee.Sources;
using Banshee.Configuration;

namespace Banshee.Collection.Gui
{
    public class PersistentColumnController : ColumnController
    {
        private string root_namespace;
        private bool loaded = false;
        private bool pending_changes;
        private uint timer_id = 0;

        private string parent_source_ns;
        private string source_ns;

        private Source source;
        public Source Source {
            get { return source; }
            set {
                if (source == value) {
                    return;
                }

                if (source != null) {
                    Save ();
                }

                source = value;
                source_ns = parent_source_ns = null;

                if (source != null) {
                    // If we have a parent, use their UniqueId as a fallback in
                    // case this source's column settings haven't been changed
                    // from its parents
                    parent_source_ns = String.Format ("{0}.{1}.", root_namespace, source.ParentConfigurationId);
                    source_ns = String.Format ("{0}.{1}.", root_namespace, source.ConfigurationId);
                    Load ();
                }
            }
        }

        public PersistentColumnController (string rootNamespace) : base ()
        {
            if (String.IsNullOrEmpty (rootNamespace)) {
                throw new ArgumentException ("Argument must not be null or empty", "rootNamespace");
            }

            root_namespace = rootNamespace;
        }

        public void Load ()
        {
            lock (this) {
                if (source == null) {
                    return;
                }

                loaded = false;

                int i = 0;
                foreach (Column column in this) {
                    if (column.Id != null) {
                        column.Visible = Get<bool> (column.Id, "visible", column.Visible);
                        column.Width = Get<double> (column.Id, "width", column.Width);
                        column.OrderHint = Get<int> (column.Id, "order", i);
                    } else {
                        column.OrderHint = -1;
                    }
                    i++;
                }

                Columns.Sort ((a, b) => a.OrderHint.CompareTo (b.OrderHint));

                string sort_column_id = Get<string> ("sort", "column", null);
                if (sort_column_id != null) {
                    ISortableColumn sort_column = null;
                    foreach (Column column in this) {
                        if (column.Id == sort_column_id) {
                            sort_column = column as ISortableColumn;
                            break;
                        }
                    }

                    if (sort_column != null) {
                        int sort_dir = Get<int> ("sort", "direction", 0);
                        SortType sort_type = sort_dir == 0 ? SortType.None : sort_dir == 1 ? SortType.Ascending : SortType.Descending;
                        sort_column.SortType = sort_type;
                        base.SortColumn = sort_column;
                    }
                } else {
                    base.SortColumn = null;
                }

                loaded = true;
            }

            OnUpdated ();
        }

        public override ISortableColumn SortColumn {
            set { base.SortColumn = value; Save (); }
        }

        public void Save ()
        {
            if (timer_id == 0) {
                timer_id = GLib.Timeout.Add (500, OnTimeout);
            } else {
                pending_changes = true;
            }
        }

        private bool OnTimeout ()
        {
            if (pending_changes) {
                pending_changes = false;
                return true;
            } else {
                SaveCore ();
                timer_id = 0;
                return false;
            }
        }

        private void SaveCore ()
        {
            lock (this) {
                if (source == null) {
                    return;
                }

                for (int i = 0; i < Count; i++) {
                    if (Columns[i].Id != null) {
                        Save (Columns[i], i);
                    }
                }

                if (SortColumn != null) {
                    Set<string> ("sort", "column", SortColumn.Id);
                    Set<int> ("sort", "direction", (int)SortColumn.SortType);
                }
            }
        }

        private void Set<T> (string ns, string key, T val)
        {
            string conf_ns = source_ns + ns;
            T result;

            if (source_ns != parent_source_ns) {
                if (!ConfigurationClient.TryGet<T> (conf_ns, key, out result) &&
                    val != null && val.Equals (ConfigurationClient.Get<T> (parent_source_ns + ns, key, default(T)))) {
                    conf_ns = null;
                }
            }

            if (conf_ns != null) {
                ConfigurationClient.Set<T> (conf_ns, key, val);
            }
        }

        private T Get<T> (string ns, string key, T fallback)
        {
            T result;
            if (ConfigurationClient.TryGet<T> (source_ns + ns, key, out result)) {
                return result;
            }

            if (source_ns != parent_source_ns) {
                return ConfigurationClient.Get<T> (parent_source_ns + ns, key, fallback);
            }

            return fallback;
        }

        private void Save (Column column, int index)
        {
            Set<int> (column.Id, "order", index);
            Set<bool> (column.Id, "visible", column.Visible);
            Set<double> (column.Id, "width", column.Width);
        }

        protected override void OnWidthsChanged ()
        {
            if (loaded) {
                Save ();
            }

            base.OnWidthsChanged ();
        }

        public override bool EnableColumnMenu {
            get { return true; }
        }
    }
}
