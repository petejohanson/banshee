//
// TaglibReadWriteTests.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.IO;
using System.Reflection;
using NUnit.Framework;

using Banshee.Collection;
using Banshee.Streaming;
using Banshee.Configuration.Schema;

using Hyena;
using Hyena.Tests;

namespace Banshee.Metadata
{
    // FIXME: These tests don't really belong here

    [TestFixture]
    public class TaglibReadWriteTests : TestBase
    {
        private string [] files;

        [TestFixtureSetUp]
        public void Setup ()
        {
            files = new string [] {
                Path.Combine (TestsDir, "data/test.mp3")
            };
        }

        [Test]
        public void TestChangeTrack ()
        {
            foreach (var p in Banshee.IO.Tests.Providers) {
                Banshee.IO.Provider.SetProvider (p);
                WriteMetadata (files, ChangeTrack, VerifyTrack);
            }
        }

        [Test]
        public void TestGenre ()
        {
            foreach (var p in Banshee.IO.Tests.Providers) {
                Banshee.IO.Provider.SetProvider (p);
                WriteMetadata (files, delegate (TrackInfo track) {
                    ChangeTrack (track);
                    track.Genre = "My Genre";
                }, delegate (TrackInfo track) {
                    VerifyTrack (track);
                    Assert.AreEqual ("My Genre", track.Genre);
                });
            }
        }

        [Test]
        public void TestNullGenreBug ()
        {
            foreach (var p in Banshee.IO.Tests.Providers) {
                Banshee.IO.Provider.SetProvider (p);
                // Bug in taglib-sharp-2.0.3.0: Crash if you send it a genre of "{ null }" on
                // a song with both ID3v1 and ID3v2 metadata. It's happy with "{}", though.
                // (see http://forum.taglib-sharp.com/viewtopic.php?f=5&t=239 )
                // This tests our workaround.
                WriteMetadata (files, delegate (TrackInfo track) {
                    ChangeTrack (track);
                    track.Genre = null;
                }, delegate (TrackInfo track) {
                    VerifyTrack (track);
                    Assert.IsNull (track.Genre);
                });
            }
        }

        [Test]
        public void TestIsCompilation ()
        {
            foreach (var p in Banshee.IO.Tests.Providers) {
                Banshee.IO.Provider.SetProvider (p);

                WriteMetadata (files, delegate (TrackInfo track) {
                    ChangeTrack (track);
                    // bgo#563283: IsCompilation was reset if AlbumArtist == Artist
                    track.AlbumArtist = track.ArtistName;
                    track.IsCompilation = true;
                }, delegate (TrackInfo track) {
                    VerifyTrack (track);
                    Assert.AreEqual (track.ArtistName, track.AlbumArtist);
                    Assert.IsTrue (track.IsCompilation);
                });
            }
        }

        [Test]
        public void TestIsNotCompilation ()
        {
            foreach (var p in Banshee.IO.Tests.Providers) {
                Banshee.IO.Provider.SetProvider (p);
                WriteMetadata (files, delegate (TrackInfo track) {
                    ChangeTrack (track);
                    track.AlbumArtist = track.ArtistName;
                    track.IsCompilation = false;
                }, delegate (TrackInfo track) {
                    VerifyTrack (track);
                    Assert.AreEqual (track.ArtistName, track.AlbumArtist);
                    Assert.IsFalse (track.IsCompilation);
                });
            }
        }

        [Test]
        public void TestIsCompilationAndAlbumArtist ()
        {
            foreach (var p in Banshee.IO.Tests.Providers) {
                Banshee.IO.Provider.SetProvider (p);
                WriteMetadata (files, delegate (TrackInfo track) {
                    ChangeTrack (track);
                    track.AlbumArtist = "My Album Artist";
                    track.IsCompilation = true;
                }, delegate (TrackInfo track) {
                    VerifyTrack (track);
                    Assert.AreEqual ("My Album Artist", track.AlbumArtist);
                    Assert.IsTrue (track.IsCompilation);
                });
            }
        }

        private void WriteMetadata (string [] files, Action<TrackInfo> change, Action<TrackInfo> verify)
        {
            SafeUri newuri = null;
            bool write_metadata = LibrarySchema.WriteMetadata.Get();
            LibrarySchema.WriteMetadata.Set (true);
            try {
                AssertForEach<string> (files, delegate (string uri) {
                    string extension = System.IO.Path.GetExtension (uri);
                    newuri = new SafeUri (Path.Combine (TestsDir, "data/test_write." + extension));

                    Banshee.IO.File.Copy (new SafeUri (uri), newuri, true);

                    ChangeAndVerify (newuri, change, verify);
                });
            } finally {
                LibrarySchema.WriteMetadata.Set (write_metadata);
                if (newuri != null)
                    Banshee.IO.File.Delete (newuri);
            }
        }

        private void ChangeAndVerify (SafeUri uri, Action<TrackInfo> change, Action<TrackInfo> verify)
        {
            TagLib.File file = StreamTagger.ProcessUri (uri);
            TrackInfo track = new TrackInfo ();
            StreamTagger.TrackInfoMerge (track, file);
            file.Dispose ();

            // Make changes
            change (track);

            // Save changes
            bool saved = StreamTagger.SaveToFile (track, true, true);
            Assert.IsTrue (saved);

            // Read changes
            file = StreamTagger.ProcessUri (uri);
            track = new TrackInfo ();
            StreamTagger.TrackInfoMerge (track, file, false, true);
            file.Dispose ();

            // Verify changes
            verify (track);
        }

        private void ChangeTrack (TrackInfo track)
        {
            track.TrackTitle = "My Title";
            track.ArtistName = "My Artist";
            track.AlbumTitle = "My Album";
            track.TrackNumber = 4;
            track.DiscNumber = 4;
            track.Year = 1999;
            track.Rating = 2;
            track.PlayCount = 3;
        }

        private void VerifyTrack (TrackInfo track)
        {
            Assert.AreEqual ("My Title", track.TrackTitle);
            Assert.AreEqual ("My Artist", track.ArtistName);
            Assert.AreEqual ("My Album", track.AlbumTitle);
            Assert.AreEqual (4, track.TrackNumber);
            Assert.AreEqual (4, track.DiscNumber);
            Assert.AreEqual (1999, track.Year);
            Assert.AreEqual (2, track.Rating);
            Assert.AreEqual (3, track.PlayCount);
        }
    }
}

#endif
