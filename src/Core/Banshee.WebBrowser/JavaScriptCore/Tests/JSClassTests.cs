//
// JSClassTests.cs
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

#if ENABLE_TESTS

using System;
using NUnit.Framework;

namespace JavaScriptCore.Tests
{
    [TestFixture]
    public class JSClassTests
    {
        private JSContext context;
        private JSTestStaticClass static_class;
        private JSTestInstanceClass instance_class;

        [TestFixtureSetUp]
        public void Init ()
        {
            context = new JSContext ();
            static_class = new JSTestStaticClass ();
            instance_class = new JSTestInstanceClass ();

            context.GlobalObject.SetProperty ("x", new JSObject (context, static_class.ClassHandle));
            context.GlobalObject.SetProperty ("y", new JSObject (context, instance_class.ClassHandle));
        }

        private class JSTestStaticClass : JSClassDefinition
        {
            [JSStaticFunction ("return_managed_null")]
            private static JSValue ReturnManagedNull (JSObject function, JSObject thisObject, JSValue [] args)
            {
                return null;
            }

            [JSStaticFunction ("return_js_undefined")]
            private static JSValue ReturnJSUndefined (JSObject function, JSObject thisObject, JSValue [] args)
            {
                return JSValue.NewUndefined (function.Context);
            }

            [JSStaticFunction ("return_js_null")]
            private static JSValue ReturnJSNull (JSObject function, JSObject thisObject, JSValue [] args)
            {
                return JSValue.NewNull (function.Context);
            }

            [JSStaticFunction ("return_js_number")]
            private static JSValue ReturnJSNumber (JSObject function, JSObject thisObject, JSValue [] args)
            {
                return new JSValue (function.Context, 3920.6);
            }

            [JSStaticFunction ("return_js_string")]
            private static JSValue ReturnJSString (JSObject function, JSObject thisObject, JSValue [] args)
            {
                return new JSValue (function.Context, "hello world");
            }

            [JSStaticFunction ("this_object")]
            private static JSValue ThisObject (JSObject function, JSObject thisObject, JSValue [] args)
            {
                return thisObject.GetProperty ("name");
            }

            [JSStaticFunction ("args")]
            private static JSValue TestArgs (JSObject function, JSObject thisObject, JSValue [] args)
            {
                Assert.AreEqual (6, args.Length);

                Assert.AreEqual (JSType.Boolean, args[0].JSType);
                Assert.AreEqual (JSType.Number, args[1].JSType);
                Assert.AreEqual (JSType.String, args[2].JSType);
                Assert.AreEqual (JSType.Object, args[3].JSType);
                Assert.AreEqual (JSType.Null, args[4].JSType);
                Assert.AreEqual (JSType.Undefined, args[5].JSType);

                Assert.AreEqual (true, args[0].BooleanValue);
                Assert.AreEqual (42, args[1].NumberValue);
                Assert.AreEqual ("banshee", args[2].StringValue);
                Assert.IsTrue (args[3].ObjectValue.IsFunction);
                Assert.AreEqual ("args", args[3].ObjectValue.GetProperty ("name").StringValue);

                return null;
            }
        }

        [Test]
        public void TestEnsureJavaScriptCoreVoidReturnIsUndefined ()
        {
            Assert.AreEqual (JSType.Undefined, context.EvaluateScript ("(function f () { })()").JSType);
        }

        [Test]
        public void TestStaticReturnManagedNull ()
        {
            Assert.AreEqual (JSType.Undefined, context.EvaluateScript ("x.return_managed_null ()").JSType);
        }

        [Test]
        public void TestStaticReturnJSUndefined ()
        {
            Assert.AreEqual (JSType.Undefined, context.EvaluateScript ("x.return_js_undefined ()").JSType);
        }

        [Test]
        public void TestStaticReturnJSNull ()
        {
            Assert.AreEqual (JSType.Null, context.EvaluateScript ("x.return_js_null ()").JSType);
        }

        [Test]
        public void TestStaticReturnJSNumber ()
        {
            Assert.AreEqual (3920.6, context.EvaluateScript ("x.return_js_number ()").NumberValue);
        }

        [Test]
        public void TestStaticReturnJSString ()
        {
            Assert.AreEqual ("hello world", context.EvaluateScript ("x.return_js_string ()").StringValue);
        }

        [Test]
        public void TestStaticThisObject ()
        {
            Assert.AreEqual ("flipper", context.EvaluateScript ("x.this_object.call ({name:'flipper'})").StringValue);
        }

        [Test]
        public void TestStaticArgs ()
        {
            context.EvaluateScript ("x.args (true, 42, 'banshee', x.args, null, undefined)");
        }

        private class JSTestInstanceClass : JSClassDefinition
        {
            protected override JSObject OnJSCallAsConstructor (JSObject constructor, JSValue [] args)
            {
                var o = new JSObject (constructor.Context, ClassHandle);
                o.SetProperty ("hello", new JSValue (constructor.Context, "world"));
                return o;
            }
        }

        [Test]
        public void TestConstructor ()
        {
            Assert.AreEqual ("world", context.EvaluateScript ("new y ().hello").StringValue);
        }
    }
}

#endif