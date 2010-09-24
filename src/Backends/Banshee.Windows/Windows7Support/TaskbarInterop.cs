//
// TaskbarInterop.cs
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
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Windows7Support
{
    // See http://msdn.microsoft.com/en-us/library/dd391692(v=VS.85).aspx
    // for the definition of ITaskbarList3.
    // The definitions here are used for interop via COM with ITaskbarList3.

    [ComImport, Guid ("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
    [InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ITaskbarList3
    {

        #region ITaskbarList API

        [PreserveSig]
        void HrInit ();
        [PreserveSig]
        void AddTab (IntPtr hwnd);
        [PreserveSig]
        void DeleteTab (IntPtr hwnd);
        [PreserveSig]
        void ActivateTab (IntPtr hwnd);
        [PreserveSig]
        void SetActiveAlt (IntPtr hwnd);

        #endregion

        #region ITaskbarList2 API

        [PreserveSig]
        void MarkFullscreenWindow (IntPtr hwnd, [MarshalAs (UnmanagedType.Bool)] bool fFullscreen);

        #endregion

        #region ITaskbarList3 API

        [PreserveSig]
        void SetProgressState ([In] IntPtr hwnd, [In] TASKBAR_PROGRESS_STATUS flags);
        [PreserveSig]
        void SetProgressValue ([In] IntPtr hwnd, [In] ulong ullCompleted, [In] ulong ullTotal);

        [PreserveSig]
        void RegisterTab (IntPtr hwndTab, IntPtr hwndMDI);
        [PreserveSig]
        void UnregisterTab ([In] IntPtr hwndTab);
        [PreserveSig]
        void SetTabOrder ([In] IntPtr hwndTab, [In, Optional] IntPtr hwndInsertBefore);
        [PreserveSig]
        void SetTabActive ([In] IntPtr hwndTab, [In] IntPtr hwndMDI, [In] uint flags);

        [PreserveSig]
        int ThumbBarAddButtons ([In] IntPtr hwnd, [In] uint numberOfButtons, [In, MarshalAs (UnmanagedType.LPArray)] THUMBBUTTON[] buttons);
        [PreserveSig]
        int ThumbBarUpdateButtons ([In] IntPtr hwnd, [In] uint numberOfButtons, [In, MarshalAs (UnmanagedType.LPArray)] THUMBBUTTON[] buttons);
        [PreserveSig]
        int ThumbBarSetImageList ([In] IntPtr hwnd, [In] IntPtr hImageList);

        [PreserveSig]
        void SetOverlayIcon ([In] IntPtr hwnd, [In] IntPtr hIcon, [In, MarshalAs (UnmanagedType.LPWStr)] string pszDescription);

        [PreserveSig]
        void SetThumbnailTooltip ([In] IntPtr hwnd, [In, MarshalAs (UnmanagedType.LPWStr)] string pszTip);

        [PreserveSig]
        void SetThumbnailClip ([In] IntPtr hwnd, [In] IntPtr rect);

        #endregion
    }

    internal enum THUMBBUTTONMASK
    {
        THB_BITMAP = 0x1,
        THB_ICON = 0x2,
        THB_TOOLTIP = 0x4,
        THB_FLAGS = 0x8
    }

    [Flags]
    internal enum THUMBBUTTONFLAGS
    {
        THBF_ENABLED = 0x00000000,
        THBF_DISABLED = 0x00000001,
        THBF_DISMISSONCLICK = 0x00000002,
        THBF_NOBACKGROUND = 0x00000004,
        THBF_HIDDEN = 0x00000008,
        THBF_NONINTERACTIVE = 0x00000010
    }

    internal enum TASKBAR_PROGRESS_STATUS
    {
        NO_PROGRESS = 0x0,
        INDETERMINATE = 0x1,
        NORMAL = 0x2,
        ERROR = 0x4,
        Paused = 0x8
    }

    [StructLayout (LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct THUMBBUTTON
    {
        [MarshalAs (UnmanagedType.U4)]
        public THUMBBUTTONMASK dwMask;
        public UInt32 uId;
        public UInt32 iBitmap;
        public IntPtr hIcon;

        [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szTip;
        [MarshalAs (UnmanagedType.U4)]
        public THUMBBUTTONFLAGS dwFlags;
    }

    internal static class CLSID
    {
        public static readonly Guid TaskbarList = new Guid ("56FDF344-FD6D-11d0-958A-006097C9A090");
    }
}
