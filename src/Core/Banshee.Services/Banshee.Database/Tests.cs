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
using System.IO;
using NUnit.Framework;

using Hyena;

using Banshee.ServiceStack;

namespace Banshee.Database
{
    [TestFixture]
    public class DatabaseTests : Hyena.Tests.TestBase
    {
        [Test]
        public void Migrate ()
        {
            Paths.ApplicationName = Application.InternalName;

            int count = 0;
            foreach (string file in Directory.GetFiles (Path.Combine (TestsDir, "data"))) {
                if (file.EndsWith (".db")) {
                    var db_file = file + ".test-tmp-copy";
                    try {
                        File.Delete (db_file);
                        File.Copy (file, db_file);

                        // Call the magic methods to test the migration path
                        var db = new BansheeDbConnection (db_file);
                        SortKeyUpdater.Disable = true;
                        ((IInitializeService)db).Initialize ();
                        Assert.IsTrue (db.ValidateSchema ());
                        count++;
                    } catch (Exception e) {
                        Assert.Fail (String.Format ("Failed to migrate db: {0}", e));
                    } finally {
                        File.Delete (db_file);
                    }
                }
            }
            Assert.IsTrue (count > 0);
        }
    }
}

#endif
