//
// GenreDuplicateSolver.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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

using System;
using System.Linq;
using System.Collections.Generic;

using Mono.Unix;

using Hyena;
using Hyena.Data.Sqlite;

using Banshee.ServiceStack;
using Banshee.Configuration;

namespace Banshee.Fixup
{
    public class GenreDuplicateSolver : DuplicateSolver
    {
        public GenreDuplicateSolver ()
        {
            Id = "dupe-genre";
            Name = Catalog.GetString ("Duplicate Genres");
            Description = Catalog.GetString ("Displayed are genres that should likely be merged.  For each row, click the desired genre to make it bold, or uncheck it to take no action.");

            AddFinder (
                "Genre", "TrackID", "CoreTracks",
                String.Format (@"
                    TrackID IN (SELECT TrackID FROM CoreTracks
                        WHERE PrimarySourceID = {0} GROUP BY Genre
                    ) AND Genre IS NOT NULL",
                    ServiceManager.SourceManager.MusicLibrary.DbId
                ),
                "HYENA_BINARY_FUNCTION ('dupe-genre', Genre, NULL)"
            );

            BinaryFunction.Add (Id, NormalizedGroup);
        }

        public override void Dispose ()
        {
            base.Dispose ();
            BinaryFunction.Remove (Id);
        }

        private object NormalizedGroup (object genre, object null_arg)
        {
            var ret = (genre as string);
            if (ret == null)
                return null;

            ret = ret.ToLower ()
               .Replace (" and ", " & ")
               .Replace (Catalog.GetString (" and "), " & ")
               .Trim ();

            // Stips whitespace, punctuation, accents, and lower-cases
            return Hyena.StringUtil.SearchKey (ret);
        }

        public override void Fix (IEnumerable<Problem> problems)
        {
            foreach (var problem in problems) {
                ServiceManager.DbConnection.Execute (
                    "UPDATE CoreTracks SET Genre = ?, DateUpdatedStamp = ? WHERE Genre IN (?)",
                    problem.SolutionValue, DateTime.Now, problem.SolutionOptions
                );
            }
        }
    }
}
