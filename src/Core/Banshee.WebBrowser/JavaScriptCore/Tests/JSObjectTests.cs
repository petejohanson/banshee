// 
// JSObjectTests.cs
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
    public class JSObjectTests
    {
        private JSContext context;

        [TestFixtureSetUp]
        public void Init ()
        {
            context = new JSContext ();
        }

        [Test]
        public void GetPropertyOverrideTest ()
        {
            var default_obj = new JSObject (context);
            default_obj.SetProperty ("name", new JSValue (context, "abock"));

            var override_obj = new JSObject (context, new GetPropertyClassTest ().CreateClass ());
            override_obj.SetProperty ("name", new JSValue (context, "ranger"));

            Assert.AreEqual ("{\"name\":\"abock\"}", default_obj.ToJsonString (0));
            Assert.AreEqual ("{\"name\":\"always this\"}", override_obj.ToJsonString (0));
        }

        [Test]
        public void SetPropertyTest ()
        {
            var obj = new JSObject (context, null);
            var prop = new JSValue (context, "happy days");

            obj.SetProperty ("str", prop);
            Assert.IsTrue (obj.HasProperty ("str"));
            Assert.IsTrue (obj.GetProperty ("str").IsEqual (prop));
            Assert.IsTrue (obj.GetProperty ("str").IsStrictEqual (prop));
            Assert.AreEqual ("happy days", obj.GetProperty ("str").StringValue);

            obj.SetProperty ("str", obj);
            Assert.IsTrue (obj.GetProperty ("str").IsEqual (obj));
            Assert.IsTrue (obj.GetProperty ("str").IsStrictEqual (obj));
        }

        [Test]
        public void DeletePropertyTest ()
        {
            var obj = new JSObject (context, null);
            obj.SetProperty ("foo", new JSValue (context, "bar"));
            Assert.IsTrue (obj.HasProperty ("foo"));
            Assert.IsTrue (obj.DeleteProperty ("foo"));
            Assert.IsFalse (obj.HasProperty ("foo"));
            obj.SetProperty ("foo", new JSValue (context, 99));
            Assert.IsTrue (obj.HasProperty ("foo"));
            Assert.IsTrue (obj.DeleteProperty ("foo"));
            Assert.IsFalse (obj.HasProperty ("foo"));
        }

        [Test]
        public void DeleteDontDeletePropertyTest ()
        {
            var obj = new JSObject (context, null);
            obj.SetProperty ("foo", new JSValue (context, "i am permanent"), JSPropertyAttribute.DontDelete);
            Assert.IsTrue (obj.HasProperty ("foo"));
            Assert.IsFalse (obj.DeleteProperty ("foo"));
            Assert.IsTrue (obj.HasProperty ("foo"));
        }

        [Test]
        public void ReadOnlyPropertyTest ()
        {
            var obj = new JSObject (context, null);
            obj.SetProperty ("foo", new JSValue (context, "bar"), JSPropertyAttribute.ReadOnly);
            Assert.AreEqual ("bar", obj.GetProperty ("foo").StringValue);
            obj.SetProperty ("foo", new JSValue (context, "baz"));
            Assert.AreEqual ("bar", obj.GetProperty ("foo").StringValue);
            Assert.IsTrue (obj.DeleteProperty ("foo"));
            Assert.IsFalse (obj.HasProperty ("foo"));
        }

        [Test]
        public void ReadOnlyDontDeletePropertyTest ()
        {
            var obj = new JSObject (context, null);
            obj.SetProperty ("foo", new JSValue (context, "bar"),
                JSPropertyAttribute.ReadOnly | JSPropertyAttribute.DontDelete);
            Assert.AreEqual ("bar", obj.GetProperty ("foo").StringValue);
            obj.SetProperty ("foo", new JSValue (context, "baz"));
            Assert.AreEqual ("bar", obj.GetProperty ("foo").StringValue);
            Assert.IsFalse (obj.DeleteProperty ("foo"));
            Assert.IsTrue (obj.HasProperty ("foo"));
        }

        [Test]
        public void DeletePropertyScriptedTest ()
        {
            context.GlobalObject.SetProperty ("a", new JSValue (context, "apple"));
            context.GlobalObject.SetProperty ("b", new JSValue (context, "bear"));
            context.GlobalObject.SetProperty ("c", new JSValue (context, "car"));

            Assert.AreEqual ("{\"a\":\"apple\",\"b\":\"bear\",\"c\":\"car\"}",
                context.GlobalObject.ToJsonString (0));

            context.EvaluateScript ("delete b");

            Assert.AreEqual ("{\"a\":\"apple\",\"c\":\"car\"}",
                context.GlobalObject.ToJsonString (0));

            context.EvaluateScript ("this.d = a + ' ' + c");
            context.EvaluateScript ("delete a; delete c");

            Assert.AreEqual ("{\"d\":\"apple car\"}", context.GlobalObject.ToJsonString (0));

            context.EvaluateScript ("delete d");

            Assert.AreEqual ("{}", context.GlobalObject.ToJsonString (0));
        }

        [Test]
        public void PropertyEnumerationTest ()
        {
            context.EvaluateScript ("this.a = 1");
            context.EvaluateScript ("this.b = 2");
            context.EvaluateScript ("this.c = 3");
            context.EvaluateScript ("this.d = 4");
            context.EvaluateScript ("this.e = 5");

            Assert.AreEqual ("{\"a\":1,\"b\":2,\"c\":3,\"d\":4,\"e\":5}",
                context.GlobalObject.ToJsonString (0));

            context.GlobalObject.SetProperty ("f", new JSValue (context, 6));
            context.GlobalObject.SetProperty ("g", new JSValue (context, 7));
            context.GlobalObject.SetProperty ("h", new JSValue (context, 8));

            var names = context.GlobalObject.PropertyNames;

            Assert.AreEqual (8, names.Length);

            Assert.AreEqual ("a", names[0]);
            Assert.AreEqual ("b", names[1]);
            Assert.AreEqual ("c", names[2]);
            Assert.AreEqual ("d", names[3]);
            Assert.AreEqual ("e", names[4]);
            Assert.AreEqual ("f", names[5]);
            Assert.AreEqual ("g", names[6]);
            Assert.AreEqual ("h", names[7]);

            Assert.AreEqual (1, context.GlobalObject.GetProperty (names[0]).NumberValue);
            Assert.AreEqual (2, context.GlobalObject.GetProperty (names[1]).NumberValue);
            Assert.AreEqual (3, context.GlobalObject.GetProperty (names[2]).NumberValue);
            Assert.AreEqual (4, context.GlobalObject.GetProperty (names[3]).NumberValue);
            Assert.AreEqual (5, context.GlobalObject.GetProperty (names[4]).NumberValue);
            Assert.AreEqual (6, context.GlobalObject.GetProperty (names[5]).NumberValue);
            Assert.AreEqual (7, context.GlobalObject.GetProperty (names[6]).NumberValue);
            Assert.AreEqual (8, context.GlobalObject.GetProperty (names[7]).NumberValue);
        }

        private class GetPropertyClassTest : JSClassDefinition
        {
            protected override JSValue OnJSGetProperty (JSObject obj, string propertyName)
            {
                return new JSValue (obj.Context, "always this");
            }
        }
    }
}

#endif