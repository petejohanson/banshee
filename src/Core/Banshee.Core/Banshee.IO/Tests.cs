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

namespace Banshee.IO
{
    [TestFixture]
    public class Tests : TestBase
    {
        // Purposefully putting a space in this directory name, helps test uri encoding issues
        private string tmp_dir = System.IO.Path.Combine (System.Environment.CurrentDirectory, "tmp-io test-dir");
        private SafeUri foo, baz, zoo;
        private string woo, yoo;

        private void Setup ()
        {
            foo = Uri  ("foo");
            baz = Uri  ("baz");
            woo = Path ("woo");
            zoo = new SafeUri ("file://" + Path ("foo"));
            yoo = "file://" + tmp_dir;

            System.IO.Directory.CreateDirectory (tmp_dir);
            System.IO.File.WriteAllText (Path ("foo"), "bar");
            System.IO.File.WriteAllText (Path ("baz"), "oof");
            System.IO.Directory.CreateDirectory (Path ("woo"));
        }

        private void Teardown ()
        {
            try { System.IO.File.Delete (Path ("foo")); } catch {}
            try { System.IO.File.Delete (Path ("baz")); } catch {}
            try { System.IO.Directory.Delete (woo); } catch {}
            try { System.IO.Directory.Delete (tmp_dir, true); } catch {}
        }

#region File tests

        [Test]
        public void FileExists ()
        {
            ForEachProvider (() => {
                Assert.IsTrue (File.Exists (foo));
                Assert.IsTrue (File.Exists (zoo));
                Assert.IsTrue (File.Exists (baz));
            });
        }

        [Test]
        public void FileDoesntExist ()
        {
            ForEachProvider (() => {
                Assert.IsFalse (File.Exists (Uri ("woo")));
                Assert.IsFalse (File.Exists (Uri ("asd")));
            });
        }

        [Test]
        public void FileMove ()
        {
            ForEachProvider (() => {
                File.Move (foo, Uri ("fooz"));
                Assert.IsTrue  (File.Exists (Uri ("fooz")));
                Assert.IsFalse (File.Exists (foo), "Original file should not exist after being moved");
            });
        }

        [Test]
        public void FileCopy ()
        {
            ForEachProvider (() => {
                var fooz = Uri ("fooz");
                Assert.IsFalse (File.Exists (fooz));

                File.Copy (foo, fooz, false);
                Assert.IsTrue (File.Exists (fooz));
                Assert.IsTrue (File.Exists (foo), String.Format ("{0}: Original file should still exist after being copied", Provider.File));
                Assert.AreEqual (File.GetSize (foo), File.GetSize (fooz));
                Assert.AreEqual ("bar", GetContents (fooz));
            });
        }

        [Test]
        public void FileCopyWithOverwrite ()
        {
            ForEachProvider (() => {
                Assert.IsTrue (File.Exists (baz));
                Assert.AreEqual ("oof", GetContents (baz));

                File.Copy (foo, baz, true);
                Assert.IsTrue (File.Exists (baz));
                Assert.IsTrue (File.Exists (foo), String.Format ("{0}: Original file should still exist after being copied", Provider.File));
                Assert.AreEqual ("bar", GetContents (baz));
            });
        }

        [Test]
        public void FileCreate ()
        {
            ForEachProvider (() => {
                var newf = Uri ("newfile");
                Assert.IsFalse (File.Exists (newf));
                File.OpenWrite (newf, false).Close ();
                Assert.IsTrue (File.Exists (newf));

                try {
                    File.OpenWrite (newf, false).Close ();
                    Assert.Fail ("Should have thrown an exception creating already-exists file w/o overwrite");
                } catch {}

                try {
                    File.OpenWrite (newf, true).Close ();
                } catch {
                    Assert.Fail ("Should not have thrown an exception creating already-exists file w/ overwrite");
                }
            });
        }

        [Test]
        public void FileRead ()
        {
            ForEachProvider (() => {
                using (var stream = File.OpenRead (foo)) {
                    var reader = new System.IO.StreamReader (stream);
                    Assert.AreEqual ("bar", reader.ReadToEnd ());
                }
            });
        }

        [Test]
        public void FileDelete ()
        {
            ForEachProvider (() => {
                Assert.IsTrue (File.Exists (foo));
                File.Delete (foo);
                Assert.IsFalse (File.Exists (foo));
            });
        }

        [Test]
        public void FileProperties ()
        {
            ForEachProvider (() => {
                Assert.AreEqual (3, File.GetSize (foo));
                Assert.IsTrue (File.GetModifiedTime (foo) > 0);
            });
        }

#endregion

#region Directory tests

        [Test]
        public void DirectoryExists ()
        {
            ForEachProvider (() => {
                Assert.IsTrue (Directory.Exists (woo));
                Assert.IsTrue (Directory.Exists (yoo), String.Format ("Directory {0} should exist, but provider {1} says it doesn't", yoo, Provider.Directory));
            });
        }

        [Test]
        public void DirectoryDoesntExist ()
        {
            ForEachProvider (() => {
                Assert.IsFalse (Directory.Exists (Path ("foo")));
            });
        }


        [Test]
        public void DirectoryMove ()
        {
            ForEachProvider (() => {
                Directory.Move (new SafeUri (woo), Uri ("wooz"));
                Assert.IsTrue  (Directory.Exists (Path ("wooz")));
                Assert.IsFalse (Directory.Exists (woo));
            });
        }

        [Test]
        public void DirectoryCreate ()
        {
            ForEachProvider (() => {
                var newd = Path ("newdir");
                Assert.IsFalse (Directory.Exists (newd));
                Directory.Create (newd);
                Assert.IsTrue (Directory.Exists (newd));
            });
        }

#endregion

#region Demux tests

        [Test]
        public void DemuxCreateFile ()
        {
            ForEachProvider (() => {
                var newf = Uri ("newfile");
                var newp = Path ("newfile");

                File.OpenWrite (newf, false).Close ();
                Assert.IsTrue (File.Exists (newf));

                var demux = Provider.CreateDemuxVfs (newp);
                Assert.IsTrue (demux.IsWritable);
                Assert.IsTrue (demux.IsReadable);

                var stream = demux.WriteStream;
                Assert.IsTrue (stream.CanWrite);
                stream.WriteByte (0xAB);
                demux.CloseStream (stream);

                Assert.IsTrue (File.Exists (newf));
            });
        }

        [Test]
        public void DemuxOverwriteFile ()
        {
            ForEachProvider (() => {
                Assert.IsTrue (File.Exists (foo));
                Assert.AreEqual (3, File.GetSize (foo));

                var demux = Provider.CreateDemuxVfs (foo.AbsoluteUri);
                Assert.IsTrue (demux.IsWritable);
                Assert.IsTrue (demux.IsReadable);

                var stream = demux.WriteStream;
                Assert.IsTrue (stream.CanWrite);
                Assert.IsTrue (stream.CanRead);

                // Make sure can actually read from WriteStream - required by TagLib#
                // stream should contain 'bar', eg first byte == 'b'
                Assert.AreEqual (3, stream.Length);
                Assert.AreEqual ((byte)'b', stream.ReadByte (), "Error in GIO backend - shouldn't happen - fix (and the Banshee GIO backend) requires gio 2.22");
                stream.Position = 0;

                // Replace the first two bytes, and truncate the third
                stream.WriteByte (0xAB);
                stream.WriteByte (0xCD);
                stream.SetLength (2);

                // And verify those bytes are readable
                stream.Position = 1;
                Assert.AreEqual (0xCD, stream.ReadByte ());
                stream.Position = 0;
                Assert.AreEqual (0xAB, stream.ReadByte ());

                // And make sure the file is now the right size; 2 bytes
                demux.CloseStream (stream);
                Assert.IsTrue (File.Exists (foo));
                Assert.AreEqual (2, File.GetSize (foo));
            });
        }

        [Test]
        public void DemuxReadFile ()
        {
            ForEachProvider (() => {
                Assert.IsTrue (File.Exists (foo));

                var demux = Provider.CreateDemuxVfs (foo.AbsoluteUri);
                var stream = demux.ReadStream;

                // foo contains 'bar'
                Assert.AreEqual ((byte)'b', stream.ReadByte ());
                Assert.AreEqual ((byte)'a', stream.ReadByte ());
                Assert.AreEqual ((byte)'r', stream.ReadByte ());

                demux.CloseStream (stream);
            });
        }

        [Test]
        public void DirectoryDelete ()
        {
            ForEachProvider (() => {
                Assert.IsTrue (Directory.Exists (woo));
                Directory.Delete (woo);
                Assert.IsFalse (Directory.Exists (woo));
            });
        }

        [Test]
        public void DirectoryDeleteRecursive ()
        {
            ForEachProvider (() => {
                Directory.Delete (tmp_dir, true);
            });
        }

        [Test]
        public void GetChildFiles ()
        {
            ForEachProvider (() => {
                var files = Directory.GetFiles (tmp_dir).ToArray ();
                if (Provider.LocalOnly) {
                    AssertContainsSameElements (new string [] { baz.LocalPath, foo.LocalPath }, files);
                } else {
                    AssertContainsSameElements (new string [] { foo.AbsoluteUri, baz.AbsoluteUri }, files);
                }
            });
        }

        private void AssertContainsSameElements (string [] expected, string [] actual)
        {
            if (expected.Length != actual.Length) {
                throw new Exception (string.Format ("Expected {0} elements, but found {1} elements",
                                                        expected.Count (), actual.Count ()));
            }

            foreach (var item in expected) {
                if (!actual.Contains (item)) {
                    throw new Exception (string.Format ("Expected element {0} not found in actual array",
                                                            item));
                }
            }
        }

        [Test]
        public void GetChildDirs ()
        {
            ForEachProvider (() => {
                var dirs = Directory.GetDirectories (tmp_dir).ToArray ();
                if (Provider.LocalOnly) {
                    Assert.AreEqual (new string [] { new SafeUri (woo).LocalPath }, dirs);
                } else {
                    Assert.AreEqual (new string [] { new SafeUri (woo).AbsoluteUri }, dirs);
                }
            });
        }

#endregion

        private string GetContents (SafeUri uri)
        {
            var demux = Provider.CreateDemuxVfs (uri.AbsoluteUri);
            using (var stream = demux.ReadStream) {
                var reader = new System.IO.StreamReader (stream);
                return reader.ReadToEnd ();
            }
        }

        private SafeUri Uri (string filename)
        {
            return new SafeUri (Path (filename));
        }

        private string Path (string filename)
        {
            return System.IO.Path.Combine (tmp_dir, filename);
        }

        private static Banshee.IO.IProvider GetProviderFromAssembly (string name)
        {
            var asm = Assembly.LoadFrom (String.Format ("{0}/Banshee.{1}.dll", BinDir, name));
            var provider_type = asm.GetType (String.Format ("Banshee.IO.{0}.Provider", name));
            return (Banshee.IO.IProvider)Activator.CreateInstance (provider_type);
        }

        static List<IProvider> providers = new List<IProvider> ();
        static Tests ()
        {
            GLib.GType.Init ();
            Mono.Addins.AddinManager.Initialize (BinDir);
            providers.Add (new Banshee.IO.SystemIO.Provider ());
            providers.Add (GetProviderFromAssembly ("Unix"));
            try {
                providers.Add (GetProviderFromAssembly ("Gio"));
            } catch (System.IO.FileNotFoundException) {
            } catch (Exception e) {
                Console.WriteLine ("Error loading GIO backend: {0}", e);
            }
        }

        public static IEnumerable<Banshee.IO.IProvider> Providers { get { return providers; } }

        private void ForEachProvider (System.Action action)
        {
            foreach (var provider in providers) {
                Banshee.IO.Provider.SetProvider (provider);
                Setup ();
                action ();
                Teardown ();
            }
        }
    }
}

#endif
