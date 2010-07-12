// 
// OssiferCookie.cs
// 
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.InteropServices;

namespace Banshee.WebBrowser
{
    public class OssiferCookie
    {
        [StructLayout (LayoutKind.Sequential)]
        private struct NativeSoupCookie
        {
            public IntPtr name;
            public IntPtr value;
            public IntPtr domain;
            public IntPtr path;
            public IntPtr expires;
            public bool secure;
            public bool http_only;
        }

        public string Name { get; set; }
        public string Value { get; set; }
        public string Domain { get; set; }
        public string Path { get; set; }
        public bool Secure { get; set; }
        public bool HttpOnly { get; set; }

        public OssiferCookie ()
        {
        }

        public OssiferCookie (IntPtr raw)
        {
            ReadFromNative (raw);
        }

        private void ReadFromNative (IntPtr raw)
        {
            var cookie = (NativeSoupCookie)Marshal.PtrToStructure (raw, typeof (NativeSoupCookie));
            Name = GLib.Marshaller.Utf8PtrToString (cookie.name);
            Value = GLib.Marshaller.Utf8PtrToString (cookie.value);
            Domain = GLib.Marshaller.Utf8PtrToString (cookie.domain);
            Path = GLib.Marshaller.Utf8PtrToString (cookie.path);
            Secure = cookie.secure;
            HttpOnly = cookie.http_only;
        }
    }
}

