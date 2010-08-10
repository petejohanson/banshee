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

using NUnit.Framework;
using GLib;

using Hyena;

namespace Banshee.Fixup
{
    [TestFixture]
    public class FixupTests
    {
        ArtistDuplicateSolver artist_solver;

        [SetUp]
        public void Setup ()
        {
            Solver.EnableUnitTests = true;
            artist_solver = new ArtistDuplicateSolver ();
        }

        [TearDown]
        public void Teardown ()
        {
        }

        [Test]
        public void ArtistNormalization ()
        {
            AssertArtistNormalized (null, null);
            AssertArtistNormalized (null, 12);
            AssertArtistNormalized ("", "");
            AssertArtistNormalized ("foo", "foo");
            AssertArtistNormalized ("dave matthews", "Dave Matthews");
            AssertArtistNormalized ("dave matthews", "Matthews, Dave");
            AssertArtistNormalized ("black keys", "The Black Keys");
            AssertArtistNormalized ("black keys", "black Keys, the");
            AssertArtistNormalized ("beatles", "Beatles");
            AssertArtistNormalized ("beatles", "The Beatles");
            AssertArtistNormalized ("beatles", "  Béatles  , The  ");
            AssertArtistNormalized ("beatles", "Beatles, A");
            AssertArtistNormalized ("beatles", "Beatles, An");
            AssertArtistNormalized ("beatles", "A Beatles  ");
            AssertArtistNormalized ("rem", " R.É.M");
            AssertArtistNormalized ("belle and sebastian", "Belle & Sebastian");
            AssertArtistNormalized ("belle and sebastian", "Bellé and Sebastían\t ");
        }

        private void AssertArtistNormalized (string correct, object input)
        {
            Assert.AreEqual (correct, artist_solver.NormalizeArtistName (input, null));
        }
    }
}

#endif
