//
// ThumbnailToolbar.cs
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
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Windows7Support
{
    public class ThumbnailToolbar
    {
        public void SetImageList (ImageList list)
        {
            image_list = list;
            taskbar.ThumbBarSetImageList (hwnd, list.Handle);
        }

        public IList<ThumbnailToolbarButton> Buttons {
            get { return buttons; }
            set {
                if (buttons != null)
                    throw new InvalidOperationException ("The toolbar buttons can only be set once, due to a Windows limitation");

                if (value == null || value.Count == 0)
                    return;

                buttons = value;

                foreach (ThumbnailToolbarButton b in buttons)
                    b.Changed += new EventHandler (OnButtonChanged);

                IEnumerable<THUMBBUTTON> native_buttons = GetNativeButtons ();
                taskbar.ThumbBarAddButtons (hwnd, (uint)native_buttons.Count (), native_buttons.ToArray ());
            }
        }

        ImageList image_list;
        IList<ThumbnailToolbarButton> buttons;
        ITaskbarList3 taskbar;
        IntPtr hwnd;


        internal ThumbnailToolbar (IntPtr hwnd)
        {
            this.hwnd = hwnd;
            taskbar = (ITaskbarList3)Activator.CreateInstance (Type.GetTypeFromCLSID (CLSID.TaskbarList));
            taskbar.HrInit ();
        }

        void OnButtonChanged (object sender, EventArgs e)
        {
            UpdateButtons ();
        }

        IEnumerable<THUMBBUTTON> GetNativeButtons ()
        {
            return buttons.Select (b =>
            {
                THUMBBUTTONMASK mask = THUMBBUTTONMASK.THB_FLAGS;
                THUMBBUTTON native_button = new THUMBBUTTON () { uId = (uint)b.Id, dwFlags = GetFlags (b) };
                if (b.Icon != null) {
                    mask |= THUMBBUTTONMASK.THB_ICON;
                    native_button.hIcon = b.Icon.Handle;
                }

                if (!String.IsNullOrEmpty (b.Tooltip)) {
                    mask |= THUMBBUTTONMASK.THB_TOOLTIP;
                    native_button.szTip = b.Tooltip;
                }

                // TODO: ImageLIst index support.

                native_button.dwMask = mask;

                return native_button;
            }).ToList ();
        }

        void UpdateButtons ()
        {
            IEnumerable<THUMBBUTTON> native = GetNativeButtons ();

            taskbar.ThumbBarUpdateButtons (hwnd, (uint)native.Count (), native.ToArray ());
        }

        THUMBBUTTONFLAGS GetFlags (ThumbnailToolbarButton button)
        {
            THUMBBUTTONFLAGS flags = button.Enabled ? THUMBBUTTONFLAGS.THBF_ENABLED : THUMBBUTTONFLAGS.THBF_DISABLED;

            if (button.Hidden)
                flags |= THUMBBUTTONFLAGS.THBF_HIDDEN;
            if (button.NonInteractive)
                flags |= THUMBBUTTONFLAGS.THBF_NONINTERACTIVE;
            if (button.DismissOnClick)
                flags |= THUMBBUTTONFLAGS.THBF_DISMISSONCLICK;
            if (button.NoBackground)
                flags |= THUMBBUTTONFLAGS.THBF_NOBACKGROUND;

            return flags;
        }

    }
}
