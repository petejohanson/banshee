//
// GioMetadataSource.cs
//
// Author:
//   alex <${AuthorEmail}>
//
// Copyright (c) 2010 alex
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

#if ENABLE_GIO_HARDWARE
using System;

namespace Banshee.Hardware.Gio
{
    public abstract class GioMetadataSource : IMetadataSource
    {
#region IMetadataSource implementation
        public abstract string GetPropertyString (string key);

        public abstract double GetPropertyDouble (string key);

        public abstract bool GetPropertyBoolean (string key);

        public abstract int GetPropertyInteger (string key);

        public abstract ulong GetPropertyUInt64 (string key);

        public abstract string[] GetPropertyStringList (string key);

        public abstract bool PropertyExists (string key);
    }
#endregion
}

#endif
