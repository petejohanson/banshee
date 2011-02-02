//
// AudiobookFileNamePattern.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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

using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using Mono.Unix;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Configuration.Schema;

namespace Banshee.Audiobook
{
    public class AudiobookFileNamePattern : PathPattern
    {
        public AudiobookFileNamePattern ()
        {
            SuggestedFolders = new string [] {
                "%author% - %book_title%",
                "%author%%path_sep%%book_title%"
            };
            DefaultFolder = SuggestedFolders [0];

            SuggestedFiles = new string [] {
                "%title%",
                "{%disc_number% - }{%track_number% - }%title%"
            };
            DefaultFile = SuggestedFiles [0];

            AddConversion ("author", Catalog.GetString ("Author"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayAlbumArtistName);
            });

            // Translators: This means the first letter of the author's name
            AddConversion ("author_initial", Catalog.GetString("Author Initial"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayAlbumArtistName.Substring (0, 1));
            });

            AddConversion ("book_title", Catalog.GetString ("Book Title"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayAlbumTitle);
            });

            AddConversion ("title", Catalog.GetString ("Title"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayTrackTitle);
            });

            AddConversion ("year", Catalog.GetString ("Year"),
                delegate (TrackInfo t, object r) {
                    int year = t == null ? (int)r : t.Year;
                    return year > 0 ? String.Format ("{0}", year) : null;
            });

            AddConversion ("track_count", Catalog.GetString ("Count"),
                delegate (TrackInfo t, object r) {
                    int track_count = t == null ? (int)r : t.TrackCount;
                    return track_count > 0 ? String.Format ("{0:00}", track_count) : null;
            });

            AddConversion ("track_number", Catalog.GetString ("Number"),
                delegate (TrackInfo t, object r) {
                    int track_number = t == null ? (int)r : t.TrackNumber;
                    return track_number > 0 ? String.Format ("{0:00}", track_number) : null;
            });

            AddConversion ("track_count_nz", Catalog.GetString ("Count (unsorted)"),
                delegate (TrackInfo t, object r) {
                    int track_count = t == null ? (int)r : t.TrackCount;
                    return track_count > 0 ? String.Format ("{0}", track_count) : null;
            });

            AddConversion ("track_number_nz", Catalog.GetString ("Number (unsorted)"),
                delegate (TrackInfo t, object r) {
                    int track_number = t == null ? (int)r : t.TrackNumber;
                    return track_number > 0 ? String.Format ("{0}", track_number) : null;
            });

            AddConversion ("disc_count", Catalog.GetString ("Disc Count"),
                delegate (TrackInfo t, object r) {
                    int disc_count = t == null ? (int)r : t.DiscCount;
                    return disc_count > 0 ? String.Format ("{0}", disc_count) : null;
            });

            AddConversion ("disc_number", Catalog.GetString ("Disc Number"),
                delegate (TrackInfo t, object r) {
                    int disc_number = t == null ? (int)r : t.DiscNumber;
                    return disc_number > 0 ? String.Format ("{0}", disc_number) : null;
            });

            AddConversion ("grouping", Catalog.GetString ("Grouping"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.Grouping);
            });

            AddConversion ("path_sep", Path.DirectorySeparatorChar.ToString (),
                delegate (TrackInfo t, object r) {
                    return Path.DirectorySeparatorChar.ToString ();
            });
        }

        public override IEnumerable<TrackInfo> SampleTracks {
            get {
                var track = new TrackInfo () {
                    ArtistName = "J.K. Rowling",
                    AlbumTitle = "Harry Potter and the Goblet of Fire",
                    TrackTitle = "Chapter 20-09 - The First Task",
                    TrackNumber = 4,
                    TrackCount = 16,
                    DiscNumber = 9,
                    DiscCount = 17,
                    Duration = TimeSpan.FromSeconds (312),
                    Year = 2000
                };

                yield return track;
            }
        }
    }
}
