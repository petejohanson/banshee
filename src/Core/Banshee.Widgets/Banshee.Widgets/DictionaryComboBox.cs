/***************************************************************************
 *  DictionaryComboBox.cs
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
    public class DictionaryComboBox<T> : ComboBox
    {
        private ListStore store;
        private int row;

        public DictionaryComboBox ()
        {
            store = new ListStore (typeof (string), typeof (T), typeof (int));
            store.SetSortColumnId (2, SortType.Ascending);
            Model = store;

            CellRendererText text_renderer = new CellRendererText ();
            PackStart (text_renderer, true);
            AddAttribute (text_renderer, "text", 0);
        }

        public T Default { get; set; }

        public TreeIter Add (string str, T value)
        {
            return store.AppendValues (str, value, row++);
        }

        public TreeIter Add (T value, string str, int order)
        {
            return store.AppendValues (str, value, order);
        }

        public bool Remove (T value)
        {
            var iter = IterFor (value);
            return Remove (ref iter);
        }

        public bool Remove (ref TreeIter iter)
        {
            // Try to change the active value to the default
            TreeIter active_iter;
            if (GetActiveIter (out active_iter)) {
                if (active_iter.Equals (iter)) {
                    if (Default != null) {
                        ActiveValue = Default;
                    } else if (store.IterNChildren () > 0) {
                        Active = 0;
                    }
                }
            }

            return store.Remove (ref iter);
        }

        public void Update (T value, string str, int order)
        {
            var iter = IterFor (value);
            store.SetValue (iter, 0, str);
            store.SetValue (iter, 2, order);
        }

        public new void Clear ()
        {
            store.Clear ();
        }

        private TreeIter IterFor (T val)
        {
            if (val == null) {
                return TreeIter.Zero;
            }

            for (int i = 0, n = store.IterNChildren (); i < n; i++) {
                TreeIter iter;
                if (store.IterNthChild (out iter, i)) {
                    T compare = (T)store.GetValue (iter, 1);
                    if (val.Equals (compare)) {
                        return iter;
                    }
                }
            }

            return TreeIter.Zero;
        }

        public T ActiveValue {
            get {
                TreeIter iter;
                if (GetActiveIter (out iter)) {
                    return (T)store.GetValue (iter, 1);
                }

                return default (T);
            }
            set {
                var iter = IterFor (value);
                if (TreeIter.Zero.Equals (iter)) {
                    Hyena.Log.WarningFormat ("Cannot set ActiveValue to {0}, its TreeIter is null", value);
                } else {
                    SetActiveIter (iter);
                }
            }
        }
    }
}
