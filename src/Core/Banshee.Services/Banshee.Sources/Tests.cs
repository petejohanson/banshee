//
// Tests.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#if ENABLE_TESTS

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

using Banshee.Collection;
using Banshee.Streaming;
using Banshee.Configuration.Schema;

using Hyena;
using Hyena.Tests;

namespace Banshee.Sources
{
    [TestFixture]
    public class SourceManagerTests : TestBase
    {
        [Test]
        public void Dispose ()
        {
            var sm = new SourceManager ();
            sm.Initialize ();
            Assert.IsFalse (sm.Sources.Count == 0);
            sm.Dispose ();
            Assert.IsTrue (sm.Sources.Count == 0);
        }

        [Test]
        public void AddRemove ()
        {
            var sm = new SourceManager ();
            var src = new SourceManager.GroupSource ("Foo", 1);
            sm.AddSource (src, false);
            Assert.IsTrue (sm.Sources.Count == 1);

            var src2 = new SourceManager.GroupSource ("Bar", 2);
            sm.AddSource (src2, true);
            Assert.IsTrue (sm.Sources.Count == 2);
            Assert.AreEqual (src2, sm.DefaultSource);
            Assert.AreEqual (src2, sm.ActiveSource);

            sm.SetActiveSource (src);
            Assert.AreEqual (src, sm.ActiveSource);

            sm.RemoveSource (src);
            Assert.IsTrue (sm.Sources.Count == 1);
        }

        [TestFixtureSetUp]
        public void Setup ()
        {
            ThreadAssist.InitializeMainThread ();
        }
    }
}

#endif
