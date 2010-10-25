// 
// JSClassDefinition.cs
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
using System.Reflection;
using System.Runtime.InteropServices;

namespace JavaScriptCore
{
    public class JSClassDefinition
    {
        private struct JSClassDefinitionNative
        {
            public int version;
            public JSClassAttribute attributes;

            public IntPtr class_name;
            public IntPtr parent_class;

            public IntPtr /* JSStaticValue[] */ static_values;
            public IntPtr /* JSStaticFunction[] */ static_functions;

            public JSObject.InitializeCallback initialize;
            public JSObject.FinalizeCallback finalize;
            public JSObject.HasPropertyCallback has_property;
            public JSObject.GetPropertyCallback get_property;
            public JSObject.SetPropertyCallback set_property;
            public JSObject.DeletePropertyCallback delete_property;
            public JSObject.GetPropertyNamesCallback get_property_names;
            public JSObject.CallAsFunctionCallback call_as_function;
            public JSObject.CallAsConstructorCallback call_as_constructor;
            public JSObject.HasInstanceCallback has_instance;
            public JSObject.ConvertToTypeCallback convert_to_type;
        }

        private JSClassDefinitionNative raw;

        public virtual string ClassName {
            get { return GetType ().FullName.Replace (".", "_"); }
        }

        public JSClassDefinition ()
        {
            raw = new JSClassDefinitionNative ();
            raw.class_name = Marshal.StringToHGlobalAnsi (ClassName);

            Override ("OnInitialize", () => raw.initialize = new JSObject.InitializeCallback (JSInitialize));
            Override ("OnFinalize", () => raw.finalize = new JSObject.FinalizeCallback (JSFinalize));
            Override ("OnJSHasProperty", () => raw.has_property = new JSObject.HasPropertyCallback (JSHasProperty));
            Override ("OnJSGetProperty", () => raw.get_property = new JSObject.GetPropertyCallback (JSGetProperty));
            Override ("OnJSSetProperty", () => raw.set_property = new JSObject.SetPropertyCallback (JSSetProperty));
            Override ("OnJSDeleteProperty", () => raw.delete_property = new JSObject.DeletePropertyCallback (JSDeleteProperty));
            Override ("OnJSGetPropertyNames", () => raw.get_property_names = new JSObject.GetPropertyNamesCallback (JSGetPropertyNames));
        }

        private void Override (string methodName, Action handler)
        {
            var method = GetType ().GetMethod (methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method != null && (method.Attributes & MethodAttributes.VtableLayoutMask) == 0) {
                handler ();
            }
        }

        [DllImport (JSContext.NATIVE_IMPORT)]
        private static extern IntPtr JSClassCreate (ref JSClassDefinition.JSClassDefinitionNative definition);

        public JSClass CreateClass ()
        {
            return new JSClass (JSClassCreate (ref raw));
        }

        private void JSInitialize (IntPtr ctx, IntPtr obj)
        {
            OnJSInitialize (new JSObject (ctx, obj));
        }

        protected virtual void OnJSInitialize (JSObject obj)
        {
        }

        private void JSFinalize (IntPtr obj)
        {
            OnJSFinalize (new JSObject (obj));
        }

        protected virtual void OnJSFinalize (JSObject obj)
        {
        }

        private bool JSHasProperty (IntPtr ctx, IntPtr obj, JSString propertyName)
        {
            return OnJSHasProperty (new JSObject (ctx, obj), propertyName.Value);
        }

        protected virtual bool OnJSHasProperty (JSObject obj, string propertyName)
        {
            return false;
        }

        private IntPtr JSGetProperty (IntPtr ctx, IntPtr obj, JSString propertyName, ref IntPtr exception)
        {
            var context = new JSContext (ctx);
            return (OnJSGetProperty (new JSObject (context, obj),
                propertyName.Value) ?? JSValue.NewNull (context)).Raw;
        }

        protected virtual JSValue OnJSGetProperty (JSObject obj, string propertyName)
        {
            return JSValue.NewUndefined (obj.Context);
        }

        private bool JSSetProperty (IntPtr ctx, IntPtr obj, JSString propertyName,
            IntPtr value, ref IntPtr exception)
        {
            var context = new JSContext (ctx);
            return OnJSSetProperty (new JSObject (context, obj), propertyName.Value, new JSValue (context, value));
        }

        protected virtual bool OnJSSetProperty (JSObject obj, string propertyName, JSValue value)
        {
            return false;
        }

        private bool JSDeleteProperty (IntPtr ctx, IntPtr obj, JSString propertyName, ref IntPtr exception)
        {
            return OnJSDeleteProperty (new JSObject (ctx, obj), propertyName.Value);
        }

        protected virtual bool OnJSDeleteProperty (JSObject obj, string propertyName)
        {
            return false;
        }

        private void JSGetPropertyNames (IntPtr ctx, IntPtr obj, JSPropertyNameAccumulator propertyNames)
        {
            OnJSGetPropertyNames (new JSObject (ctx, obj), propertyNames);
        }

        protected virtual void OnJSGetPropertyNames (JSObject obj, JSPropertyNameAccumulator propertyNames)
        {
        }
    }
}
