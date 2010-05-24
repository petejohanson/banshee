//
// SpinButtonEntry.cs
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
using Gtk;

namespace Banshee.Gui.TrackEditor
{
    public class SpinButtonEntry : SpinButton, IEditorField
    {
        public new event EventHandler Changed {
            add { ValueChanged += value; }
            remove { ValueChanged -= value; }
        }

        public SpinButtonEntry (double min, double max, double step) : base (min, max, step)
        {
        }

        public void SetAsReadOnly ()
        {
            IsEditable = false;
        }

        //FIXME: workaround for BGO#611825, remove it when it's fixed:
        public new double Value {
            set {
                base.Value = value;
                if (!IsEditable) {
                    Adjustment = new Adjustment (value, value, value, 0, 0, 0);
                }
            }
            get { return base.Value; }
        }

        // Make sure the value is updated every time the text is changed, not just when the focus leaves
        // this SpinButton, since that may be too late
        protected override void OnChanged ()
        {
            // Don't update when empty, since it will be treated as a 0 which will get inserted.
            // Particularly messes up selecting all text and typing over it.
            if (Text.Trim () != "") {
                Update ();
            }
        }
    }
}
