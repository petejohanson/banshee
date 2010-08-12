//
// ArtistDuplicateSolver.cs
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
    public class ArtistDuplicateSolver : DuplicateSolver
    {
        public ArtistDuplicateSolver ()
        {
            Id = "dupe-artist";
            Name = Catalog.GetString ("Duplicate Artists");
            Description = Catalog.GetString ("Displayed are artists that should likely be merged.  For each row, click the desired name to make it bold, or uncheck it to take no action.");

            AddFinder (
                "Name", "ArtistID", "CoreArtists",
                String.Format (
                    @"(Name IS NOT NULL AND ArtistID IN (SELECT DISTINCT(ArtistID) FROM CoreTracks WHERE PrimarySourceID = {0})
                        OR ArtistID IN (SELECT DISTINCT(a.ArtistID) FROM CoreTracks t, CoreAlbums a WHERE t.AlbumID = a.AlbumID AND t.PrimarySourceID = {0}))",
                    EnableUnitTests ? 0 : ServiceManager.SourceManager.MusicLibrary.DbId
                ),
                "HYENA_BINARY_FUNCTION ('dupe-artist', Name, NULL)"
            );

            BinaryFunction.Add (Id, NormalizeArtistName);
        }

        public override void Dispose ()
        {
            base.Dispose ();
            BinaryFunction.Remove (Id);
        }

        private string comma = ", ";
        private string [] comma_ary = new string [] { ", " };

        internal object NormalizeArtistName (object name, object null_arg)
        {
            var ret = name as string;
            if (ret == null)
                return null;

            // If has only one comma, split on it and reverse the order
            // eg Matthews, Dave => Dave Matthews
            int i = ret.IndexOf (comma);
            if (i != -1 && i == ret.LastIndexOf (comma)) {
                ret = ret.Split (comma_ary, StringSplitOptions.None)
                         .Reverse ()
                         .Join (" ");
            }

            ret = ret.ToLower ()
                     .RemovePrefixedArticles ()
                     .RemoveSuffixedArticles ()
                     .NormalizeConjunctions ();

            // Strip extra whitespace, punctuation, and accents, lower-case, etc
            ret = Hyena.StringUtil.SearchKey (ret).Trim ();
            return ret;
        }

        public override void Fix (IEnumerable<Problem> problems)
        {
            foreach (var problem in problems) {
                // OK, we're combining two or more artists into one.  To do that,
                // we need to associate all the tracks and albums onto the the
                // that will remain -- the one with Name == problem.SolutionValue.
                // So, separate the ID of the winner from the rest
                var winner_id = problem.ObjectIds [Array.IndexOf (problem.SolutionOptions, problem.SolutionValue)];
                var losers = problem.ObjectIds.Where (id => id != winner_id).ToArray ();

                // FIXME update MetadataHash as well
                ServiceManager.DbConnection.Execute (
                    @"UPDATE CoreAlbums SET ArtistID = ? WHERE ArtistID IN (?);
                      UPDATE CoreTracks SET ArtistID = ? WHERE ArtistID IN (?);
                      UPDATE CoreTracks SET DateUpdatedStamp = ? WHERE
                        ArtistID = ? OR AlbumID IN (SELECT AlbumID FROM CoreAlbums WHERE ArtistID = ?)",
                    winner_id, losers,
                    winner_id, losers,
                    DateTime.Now, winner_id, winner_id
                );
            }
        }
    }
}
