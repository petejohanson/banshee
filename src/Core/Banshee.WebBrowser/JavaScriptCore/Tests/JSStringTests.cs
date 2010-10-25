//
// JSStringTests.cs
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
    public class JSStringTests
    {
        [Test]
        public void Equality ()
        {
            var a_m = "A simple greeting";
            var a_j = JSString.New (a_m);
            Assert.AreEqual (a_m, a_j.Value);

            var b_m = a_j.Value;
            var b_j = JSString.New (b_m);
            Assert.IsTrue (a_j.IsEqual (b_j));
        }

        [Test]
        public void LengthAndEquality ()
        {
            var a_m = "Hello World";
            var a_j = JSString.New (a_m);
            var b_j = JSString.New (a_j.Value);
            Assert.AreEqual (a_m.Length, a_j.Length);
            Assert.AreEqual (a_j.Length, b_j.Length);
            Assert.AreEqual (a_m, a_j.Value);
            Assert.AreEqual (a_j.Value, b_j.Value);
            Assert.IsTrue (a_j.IsEqual (b_j));
        }
    }
}

#endif
