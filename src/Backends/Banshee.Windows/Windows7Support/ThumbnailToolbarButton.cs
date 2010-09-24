//
// ThumbnailToolbarButton.cs
//
// Authors:
//   Pete Johanson <peter@peterjohanson.com>
//
// Copyright (C) 2010 Pete Johanson
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
using System.Linq;
using System.Drawing;
using System.Text;

namespace Windows7Support
{
    public class ThumbnailToolbarButton
    {
        public event EventHandler Clicked;
        public event EventHandler Changed;

        public Int16 Id { get; private set; }

        int image_index = -1;
        public int ImageIndex {
            get { return image_index; }
            set {
                if (image_index == value)
                    return;

                image_index = value;

                FireChanged ();
            }
        }

        Icon icon;
        public Icon Icon {
            get { return icon; }
            set {
                if (icon == value)
                    return;

                icon = value;
                FireChanged ();
            }
        }

        string tooltip;
        public string Tooltip {
            get { return tooltip; }
            set {
                if (tooltip == value)
                    return;

                if (tooltip != null && tooltip.Length > 260)
                    throw new ArgumentException ("Tooltip cannot be greater than 260 characters long.");

                tooltip = value;
                FireChanged ();
            }
        }

        bool enabled = true;
        public bool Enabled {
            get { return enabled; }
            set {
                if (enabled == value)
                    return;

                enabled = value;
                FireChanged ();
            }
        }

        bool dismiss_on_click = true;
        public bool DismissOnClick {
            get { return dismiss_on_click; }
            set {
                if (dismiss_on_click == value)
                    return;

                dismiss_on_click = value;
                FireChanged ();
            }
        }

        bool hidden;
        public bool Hidden {
            get { return hidden; }
            set {
                if (hidden == value)
                    return;

                hidden = value;
                FireChanged ();
            }
        }

        bool non_interactive;
        public bool NonInteractive {
            get { return non_interactive; }
            set {
                if (non_interactive == value)
                    return;

                non_interactive = value;
                FireChanged ();
            }
        }

        bool no_background = false;
        public bool NoBackground {
            get { return no_background; }
            set {
                if (no_background == value)
                    return;

                no_background = value;
                FireChanged ();
            }
        }

        public ThumbnailToolbarButton (Int16 id)
        {
            Id = id;
        }

        internal void FireClicked ()
        {
            var handler = Clicked;
            if (handler != null)
                handler (this, EventArgs.Empty);
        }

        void FireChanged ()
        {
            var handler = Changed;
            if (handler != null)
                handler (this, EventArgs.Empty);
        }
    }
}
