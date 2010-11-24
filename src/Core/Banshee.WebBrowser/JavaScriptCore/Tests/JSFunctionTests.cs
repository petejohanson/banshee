// 
// JSFunctionTests.cs
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
using System.Collections.Generic;
using NUnit.Framework;

namespace JavaScriptCore.Tests
{
    [TestFixture]
    public class JSFunctionTests
    {
        private JSContext context;

        [TestFixtureSetUp]
        public void Init ()
        {
            context = new JSContext ();
        }

        [Test]
        public void TestArguments ()
        {
            var r = new List<JSValue> ();

            context.GlobalObject.SetProperty ("go", new JSFunction (context, "go", (fn, ths, args) => {
                r.AddRange (args);
                return new JSValue (fn.Context, 42);
            }));

            var go = context.GlobalObject.GetProperty ("go");
            Assert.AreEqual (JSType.Object, go.JSType);
            Assert.AreEqual ("function go() {\n    [native code]\n}", go.ToString ());

            context.EvaluateScript ("this.go = go (1, 'hi', false, go ({a:'str', b:go ()}, go (0.272, true, go)))");

            Assert.AreEqual (JSType.Number, r[0].JSType);
            Assert.AreEqual (0.272, r[0].NumberValue);
            Assert.AreEqual (JSType.Boolean, r[1].JSType);
            Assert.AreEqual (true, r[1].BooleanValue);
            Assert.AreEqual (JSType.Object, r[2].JSType);
            Assert.AreEqual ("function go() {\n    [native code]\n}", r[2].ToString ());

            Assert.AreEqual (JSType.Object, r[3].JSType);
            Assert.AreEqual ("{\"a\":\"str\",\"b\":42}", r[3].ToString ());
            Assert.AreEqual (JSType.Number, r[4].JSType);
            Assert.AreEqual (42, r[4].NumberValue);

            Assert.AreEqual (JSType.Number, r[5].JSType);
            Assert.AreEqual (1, r[5].NumberValue);
            Assert.AreEqual (JSType.String, r[6].JSType);
            Assert.AreEqual ("hi", r[6].StringValue);
            Assert.AreEqual (JSType.Boolean, r[7].JSType);
            Assert.AreEqual (false, r[7].BooleanValue);

            go = context.GlobalObject.GetProperty ("go");
            Assert.AreEqual (JSType.Number, go.JSType);
            Assert.AreEqual (42, go.NumberValue);
        }

        private JSValue Fib_1 (JSObject function, JSObject @this, JSValue [] args)
        {
            return args[0].NumberValue <= 1 ? new JSValue (@this.Context, 1) : new JSValue (@this.Context,
                Fib_1 (function, @this, new [] { new JSValue (@this.Context, args[0].NumberValue - 1) }).NumberValue +
                Fib_1 (function, @this, new [] { new JSValue (@this.Context, args[0].NumberValue - 2) }).NumberValue
            );
        }

        private int Fib_2 (int n)
        {
            return n <= 1 ? 1 : Fib_2 (n - 1) + Fib_2 (n - 2);
        }

        [Test]
        public void TestRecursion ()
        {
            context.GlobalObject.SetProperty ("fib", new JSFunction (context, null, Fib_1));
            for (int i = 0; i <= 25; i++) {
                context.EvaluateScript ("this.fib_" + i + " = fib (" + i + ")");
                Assert.AreEqual (Fib_2 (i), context.GlobalObject.GetProperty ("fib_" + i).NumberValue);
            }
        }

        [Test]
        public void TestRecursionNoScript ()
        {
            var fib = new JSFunction (context, null, Fib_1);
            for (int i = 0; i <= 25; i++) {
                Assert.AreEqual (Fib_2 (i), fib.CallAsFunction (null, new [] { new JSValue (context, i) }).NumberValue);
            }
        }
    }
}

#endif