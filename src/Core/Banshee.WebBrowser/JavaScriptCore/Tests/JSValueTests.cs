// 
// JSValueTests.cs
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
    public class JSValueTests
    {
        private JSContext context;

        [TestFixtureSetUp]
        public void Init ()
        {
            context = new JSContext ();
        }

        [Test]
        public void Number ()
        {
            var a = new JSValue (context, 10);
            var b = new JSValue (context, 10.0);
            var c = new JSValue (context, 10.0f);
            var d = new JSValue (context, 0);
            var e = new JSValue (context, -200);

            Assert.IsTrue (a.IsNumber);
            Assert.IsTrue (b.IsNumber);
            Assert.IsTrue (c.IsNumber);
            Assert.IsTrue (d.IsNumber);
            Assert.IsTrue (e.IsNumber);

            Assert.AreEqual (10.0, a.NumberValue);
            Assert.AreEqual (10.0, b.NumberValue);
            Assert.AreEqual (10.0, c.NumberValue);
            Assert.AreEqual (0, d.NumberValue);
            Assert.AreEqual (-200, e.NumberValue);

            Assert.AreEqual (false, d.BooleanValue);
            Assert.AreEqual (true, e.BooleanValue);

            Assert.AreEqual ("-200", e.StringValue);

            Assert.AreEqual (JSType.Number, a.JSType);
            Assert.AreEqual (JSType.Number, b.JSType);
            Assert.AreEqual (JSType.Number, c.JSType);
            Assert.AreEqual (JSType.Number, d.JSType);
            Assert.AreEqual (JSType.Number, e.JSType);
        }

        [Test]
        public void Boolean ()
        {
            var a = new JSValue (context, false);
            var b = new JSValue (context, true);

            Assert.IsTrue (a.IsBoolean);
            Assert.IsTrue (b.IsBoolean);

            Assert.AreEqual (false, a.BooleanValue);
            Assert.AreEqual (true, b.BooleanValue);

            Assert.AreEqual (0.0, a.NumberValue);
            Assert.AreEqual (1.0, b.NumberValue);

            Assert.AreEqual ("false", a.StringValue);
            Assert.AreEqual ("true", b.StringValue);

            Assert.AreEqual (JSType.Boolean, a.JSType);
            Assert.AreEqual (JSType.Boolean, b.JSType);
        }

        [Test]
        public void String ()
        {
            var a = new JSValue (context, "Hello World");
            var b = new JSValue (context, JSString.New ("Hello World"));

            Assert.IsTrue (a.IsString);
            Assert.IsTrue (b.IsString);

            Assert.IsTrue (a.IsEqual (b));
            Assert.IsTrue (a.IsStrictEqual (b));
            Assert.AreEqual (a.StringValue, b.StringValue);

            Assert.AreEqual (JSType.String, a.JSType);
            Assert.AreEqual (JSType.String, b.JSType);

            a = new JSValue (context, "Hello World");
            b = new JSValue (context, JSString.New ("Not the same"));

            Assert.IsFalse (a.IsEqual (b));
            Assert.IsFalse (a.IsStrictEqual (b));
        }

        [Test]
        public void Null ()
        {
            var a = JSValue.NewNull (context);
            Assert.IsTrue (a.IsNull);
            Assert.AreEqual (JSType.Null, a.JSType);
            Assert.AreEqual (false, a.BooleanValue);
            Assert.AreEqual (0, a.NumberValue);
            Assert.AreEqual ("null", a.StringValue);
        }

        [Test]
        public void Undefined ()
        {
            var a = JSValue.NewUndefined (context);
            Assert.IsTrue (a.IsUndefined);
            Assert.AreEqual (JSType.Undefined, a.JSType);
            Assert.AreEqual (false, a.BooleanValue);
            Assert.AreEqual (Double.NaN, a.NumberValue);
            Assert.AreEqual ("undefined", a.StringValue);
        }

        [Test]
        public void EmptyEquality ()
        {
            var undef_v = JSValue.NewUndefined (context);
            var null_v = JSValue.NewNull (context);
            var number_v = new JSValue (context, 0);
            var bool_v = new JSValue (context, false);
            var string_v = new JSValue (context, "");

            Assert.IsTrue (undef_v.IsEqual (undef_v));
            Assert.IsTrue (undef_v.IsStrictEqual (undef_v));
            Assert.IsTrue (undef_v.IsEqual (null_v));
            Assert.IsFalse (undef_v.IsStrictEqual (null_v));
            Assert.IsFalse (undef_v.IsEqual (number_v));
            Assert.IsFalse (undef_v.IsStrictEqual (number_v));
            Assert.IsFalse (undef_v.IsEqual (bool_v));
            Assert.IsFalse (undef_v.IsStrictEqual (bool_v));
            Assert.IsFalse (undef_v.IsEqual (string_v));
            Assert.IsFalse (undef_v.IsStrictEqual (string_v));

            Assert.IsFalse (string_v.IsEqual (undef_v));
            Assert.IsFalse (string_v.IsStrictEqual (undef_v));
            Assert.IsFalse (string_v.IsEqual (null_v));
            Assert.IsFalse (string_v.IsStrictEqual (null_v));
            Assert.IsTrue (string_v.IsEqual (number_v));
            Assert.IsFalse (string_v.IsStrictEqual (number_v));
            Assert.IsTrue (string_v.IsEqual (bool_v));
            Assert.IsFalse (string_v.IsStrictEqual (bool_v));
            Assert.IsTrue (string_v.IsEqual (string_v));
            Assert.IsTrue (string_v.IsStrictEqual (string_v));
        }

        [Test]
        public void FromJsonNumber ()
        {
            var a = JSValue.FromJson (context, "4.5");
            var b = JSValue.FromJson (context, "-200");
            var c = JSValue.FromJson (context, "0");
            var d = JSValue.FromJson (context, "2e5");

            Assert.IsTrue (a.IsNumber);
            Assert.IsTrue (b.IsNumber);
            Assert.IsTrue (c.IsNumber);
            Assert.IsTrue (d.IsNumber);

            Assert.AreEqual (4.5, a.NumberValue);
            Assert.AreEqual (-200, b.NumberValue);
            Assert.AreEqual (0, c.NumberValue);
            Assert.AreEqual (2e5, d.NumberValue);
        }

        [Test]
        public void FromJsonBoolean ()
        {
            var a = JSValue.FromJson (context, "false");
            var b = JSValue.FromJson (context, "true");

            Assert.IsTrue (a.IsBoolean);
            Assert.IsTrue (b.IsBoolean);

            Assert.AreEqual (false, a.BooleanValue);
            Assert.AreEqual (true, b.BooleanValue);
        }

        [Test]
        public void FromJsonString ()
        {
            var a = JSValue.FromJson (context, "\"hello\"");
            Assert.IsTrue (a.IsString);
            Assert.AreEqual ("hello", a.StringValue);
        }

        [Test]
        public void FromJsonObject ()
        {
            Assert.IsTrue (JSValue.FromJson (context, "{}").IsObject);
        }

        [Test]
        [ExpectedException (typeof (JSException))]
        public void InvalidJson ()
        {
            JSValue.FromJson (context, "{x/2");
        }

        [Test]
        public void JsonRoundTrip ()
        {
            Assert.AreEqual ("[1,2,3]", JSValue.FromJson (context, "[1,2,3]").ToJsonString (0));
            Assert.AreEqual ("{\n  \"x\": 4,\n  \"y\": \"z\"\n}",
                JSValue.FromJson (context, "{\"x\":4,\"y\":\"z\"}").ToJsonString (2));
        }

        [Test]
        public void ProtectUnprotect ()
        {
            var a = new JSValue (context, 5);
            a.Protect ();
            a.Unprotect ();
        }
    }
}

#endif
