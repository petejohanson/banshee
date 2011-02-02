// 
// JSException.cs
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

namespace JavaScriptCore
{
    public class JSException : Exception
    {
        public JSContext Context { get; private set; }
        public JSValue Error { get; private set; }

        internal JSException (JSContext context, JSValue error)
            : base ("JSON: " + error.ToString ())
        {
            Error = error;
            Context = context;
        }

        internal JSException (JSContext context, string message) : base (message)
        {
            Context = context;
        }

        internal JSException (JSContext context, IntPtr exception)
            : this (context, new JSValue (context, exception))
        {
        }

        internal static void Proxy (JSContext ctx, IntPtr exception)
        {
            if (!exception.Equals (IntPtr.Zero)) {
                throw new JSException (ctx, exception);
            }
        }
    }
}
