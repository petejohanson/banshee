//
// SerializerTests.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (c) 2010 Novell, Inc.
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
using System.Linq;
using System.Reflection;
using NUnit.Framework;

using Hyena.Json;
using System.Collections;

namespace Hyena.Json.Tests
{
    [TestFixture]
    public class SerializerTests : Hyena.Tests.TestBase
    {
        JsonObject obj;
        const string obj_serialized = "{\n  \"foo\" : bar\n  \"baz\" : 12,2\n}\n";
        const string array_serialized = "[\n  foo\n  {\n    \"foo\" : bar\n    \"baz\" : 12,2\n  }\n]\n";

        [TestFixtureSetUp]
        public void Setup ()
        {
            obj = new JsonObject ();
            obj["foo"] = "bar";
            obj["baz"] = 12.2;
        }

        [Test]
        public void Literal ()
        {
            Assert.AreEqual (obj_serialized, obj.ToString ());
        }

        [Test]
        public void Array ()
        {
            var empty = new JsonArray ();
            Assert.AreEqual ("[ ]\n", empty.ToString ());

            empty.Add (new JsonArray ());
            Assert.AreEqual ("[\n  [ ]\n]\n", empty.ToString ());

            empty.Add (new JsonObject ());
            Assert.AreEqual ("[\n  [ ]\n  { }\n]\n", empty.ToString ());

            var a = new JsonArray ();
            a.Add ("foo");
            a.Add (obj);
            Assert.AreEqual (array_serialized, a.ToString ());
        }

        [Test]
        public void ExtensionMethods ()
        {
            Assert.AreEqual (
                "[\n  0\n  1\n  2\n  3\n]\n",
                Enumerable.Range (0, 4).ToJsonString ()
            );

            Assert.AreEqual (
                "{\n  \"True\" : [\n    0\n    2\n  ]\n  \"False\" : [\n    1\n    3\n  ]\n}\n",
                 Enumerable.Range (0, 4).GroupBy<int, bool> (i => i % 2 == 0).ToJsonString ()
            );

            /*var a = new ArrayList ();
            a.Add (Enumerable.Range (0, 4).GroupBy<int, bool> (i => i % 2 == 0));
            Assert.AreEqual (
                "",
                a.ToJsonString ()
            );*/
        }
    }
}

#endif
