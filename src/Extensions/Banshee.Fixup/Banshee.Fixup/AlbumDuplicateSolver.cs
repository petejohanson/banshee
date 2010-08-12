//
// AlbumDuplicateSolver.cs
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
    public class AlbumDuplicateSolver : DuplicateSolver
    {
        public AlbumDuplicateSolver ()
        {
            Id = "dupe-album";
            Name = Catalog.GetString ("Duplicate Albums");
            Description = Catalog.GetString ("Displayed are albums that should likely be merged.  For each row, click the desired title to make it bold, or uncheck it to take no action.");

            AddFinder (
                "Title", "AlbumID", "CoreAlbums, CoreArtists",
                "CoreAlbums.ArtistID = CoreArtists.ArtistID AND Title IS NOT NULL AND Name IS NOT NULL AND " +
                    String.Format ("AlbumID IN (SELECT DISTINCT(AlbumID) FROM CoreTracks WHERE PrimarySourceID = {0})", ServiceManager.SourceManager.MusicLibrary.DbId),
                "HYENA_BINARY_FUNCTION ('dupe-album', Title, Name)"
            );

            BinaryFunction.Add (Id, NormalizedGroup);
        }

        public override void Dispose ()
        {
            base.Dispose ();
            BinaryFunction.Remove (Id);
        }

        private object NormalizedGroup (object album, object artist)
        {
            var ret = (album as string);
            if (ret == null || (artist as string) == null)
                return null;

            ret = ret.ToLower ()
                     .RemovePrefixedArticles ()
                     .RemoveSuffixedArticles ()
                     .NormalizeConjunctions ();

            // Strip extra whitespace, punctuation, and accents, lower-case, etc
            ret = Hyena.StringUtil.SearchKey (ret).Trim ();
            return ret + artist;
        }

        public override void Fix (IEnumerable<Problem> problems)
        {
            foreach (var problem in problems) {
                // OK, we're combining two or more albums into one.  To do that,
                // we need to associate all the tracks with the one album
                // that will remain -- the one with Title == problem.SolutionValue.
                // So, separate the ID of the winner from the rest
                var winner_id = problem.ObjectIds [Array.IndexOf (problem.SolutionOptions, problem.SolutionValue)];
                var losers = problem.ObjectIds.Where (id => id != winner_id).ToArray ();

                // FIXME update MetadataHash as well
                ServiceManager.DbConnection.Execute (
                    "UPDATE CoreTracks SET AlbumID = ?, DateUpdatedStamp = ? WHERE AlbumID IN (?)",
                    winner_id, DateTime.Now, losers
                );
            }
        }
    }
}
