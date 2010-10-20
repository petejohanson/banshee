// 
// JSObject.cs
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

namespace Ossifer.JavaScriptCore
{
    public class JSObject : JSValue
    {
        public JSObject (IntPtr raw) : base (raw)
        {
        }

        public JSObject (IntPtr context, IntPtr raw) : base (context, raw)
        {
        }

        public JSObject (JSContext context, IntPtr raw) : base (context, raw)
        {
        }

        [DllImport (JSContext.NATIVE_IMPORT)]
        private static extern IntPtr JSObjectMake (IntPtr ctx, IntPtr jsClass, IntPtr data);

        public JSObject (JSContext context) :
            base (context, JSObjectMake (context.Raw, IntPtr.Zero, IntPtr.Zero))
        {
        }

        public JSObject (JSContext context, JSClass jsClass)
            : base (context, JSObjectMake (context.Raw, jsClass.Raw, IntPtr.Zero))
        {
        }

        public delegate void
        InitializeCallback (IntPtr ctx, IntPtr obj);

        public delegate void
        FinalizeCallback (IntPtr obj);

        public delegate bool
        HasPropertyCallback (IntPtr ctx, IntPtr obj, JSString propertyName);

        public delegate IntPtr
        GetPropertyCallback (IntPtr ctx, IntPtr obj, JSString propertyName, ref IntPtr exception);
        
        public delegate bool
        SetPropertyCallback (IntPtr ctx, IntPtr obj, JSString propertyName, IntPtr value, ref IntPtr exception);
        
        public delegate bool
        DeletePropertyCallback (IntPtr ctx, IntPtr obj, JSString propertyName, ref IntPtr exception);

        public delegate void
        GetPropertyNamesCallback (IntPtr ctx, IntPtr obj, JSPropertyNameAccumulator propertyNames);
        
        public delegate IntPtr
        CallAsFunctionCallback (IntPtr ctx, IntPtr function, IntPtr thisObject, IntPtr argumentCount, IntPtr arguments, ref IntPtr exception);

        public delegate IntPtr
        CallAsConstructorCallback (IntPtr ctx, IntPtr constructor, IntPtr argumentCount, IntPtr arguments, ref IntPtr exception);

        public delegate bool
        HasInstanceCallback  (IntPtr ctx, IntPtr constructor, IntPtr possibleInstance, ref IntPtr exception);
        
        public delegate IntPtr
        ConvertToTypeCallback (IntPtr ctx, IntPtr obj, JSType type, ref IntPtr exception);

        [DllImport (JSContext.NATIVE_IMPORT)]
        private static extern void JSObjectSetProperty (IntPtr ctx, IntPtr obj, JSString propertyName,
            IntPtr value, JSPropertyAttribute attributes, ref IntPtr exception);

        public void SetProperty (JSContext context, string propertyName, JSValue value, JSPropertyAttribute attributes)
        {
            var exception = IntPtr.Zero;
            var property = JSString.New (propertyName);
            try {
                JSObjectSetProperty (context.Raw, Raw, property, value.Raw, attributes, ref exception);
                JSException.Proxy (context, exception);
            } finally {
                property.Release ();
            }
        }

        public void SetProperty (string propertyName, JSValue value)
        {
            SetProperty (Context, propertyName, value, JSPropertyAttribute.None);
        }
    }
}
