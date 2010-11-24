// 
// Runtime.cs
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

using JavaScriptCore;

namespace JavaScriptCore.Bridge
{
    public static class Runtime
    {
        public class RuntimeClassDefinition : JSClassDefinition
        {
            public override string ClassName {
                get { return "ManagedJavaScriptCore"; }
            }

            protected override bool OnJSDeleteProperty (JSObject obj, string propertyName)
            {
                throw new JSErrorException (obj.Context, "IllegalOperationError",
                    "Deleting properties on this object is not allowed");
            }

            protected override bool OnJSSetProperty (JSObject obj, string propertyName, JSValue value)
            {
                throw new JSErrorException (obj.Context, "IllegalOperationError",
                    "Setting properties on this object is not allowed");
            }

            [JSStaticFunction ("import")]
            public static JSValue Import (JSObject function, JSObject thisObject, JSValue [] args)
            {
                return null;
            }
        }

        private static RuntimeClassDefinition js_class_definition;
        private static JSClass js_class;

        static Runtime ()
        {
            js_class_definition = new RuntimeClassDefinition ();
            js_class = js_class_definition.CreateClass ();
        }

        public static void BindManagedRuntime (this JSContext context)
        {
            if (context.GlobalObject.HasProperty ("mjs")) {
                throw new ApplicationException ("Cannot bind runtime to JSContext: " +
                    "mjsc property already exists on context's global object.");
            }

            context.GlobalObject.SetProperty ("mjs", new JSObject (context, js_class),
                JSPropertyAttribute.DontDelete);
        }
    }
}

