// 
// ManagedPropertyBagClass.cs
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
using System.Collections.Generic;

namespace JavaScriptCore
{
    public class ManagedPropertyBagClass : JSClassDefinition
    {
        private Dictionary<IntPtr, Dictionary<string, JSValue>> properties
            = new Dictionary<IntPtr, Dictionary<string, JSValue>> ();

        private Dictionary<string, JSValue> GetBag (JSObject obj)
        {
            Dictionary<string, JSValue> bag;
            if (!properties.TryGetValue (obj.Raw, out bag)) {
                properties.Add (obj.Raw, bag = new Dictionary<string, JSValue> ());
            }
            return bag;
        }

        protected override void OnJSFinalize (JSObject obj)
        {
            properties.Remove (obj.Raw);
        }

        protected override bool OnJSHasProperty (JSObject obj, string propertyName)
        {
            return GetBag (obj).ContainsKey (propertyName);
        }

        protected override JSValue OnJSGetProperty (JSObject obj, string propertyName)
        {
            return GetBag (obj)[propertyName];
        }

        protected override bool OnJSSetProperty (JSObject obj, string propertyName, JSValue value)
        {
            GetBag (obj)[propertyName] = value;
            return true;
        }

        protected override bool OnJSDeleteProperty (JSObject obj, string propertyName)
        {
            return GetBag (obj).Remove (propertyName);
        }

        protected override void OnJSGetPropertyNames (JSObject obj, JSPropertyNameAccumulator propertyNames)
        {
            foreach (var name in GetBag (obj).Keys) {
                propertyNames.AddName (name);
            }
        }
    }
}

