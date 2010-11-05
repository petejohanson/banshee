//
// MusicFileNamePattern.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2009 Novell, Inc.
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

namespace Banshee.Library
{
    public class MusicFileNamePattern : PathPattern
    {
        public MusicFileNamePattern ()
        {
            DefaultFolder    = LibrarySchema.FolderPattern.DefaultValue;
            DefaultFile      = LibrarySchema.FilePattern.DefaultValue;

            SuggestedFolders = new string [] {
                DefaultFolder,
                "%album_artist%%path_sep%%album_artist% - %album%",
                "%album_artist%%path_sep%%album%{ (%year%)}",
                "%album_artist% - %album%",
                "%album%",
                "%album_artist%"
            };

            SuggestedFiles   = new string [] {
                DefaultFile,
                "{%track_number%. }%track_artist% - %title%",
                "%track_artist% - %title%",
                "%track_artist% - {%track_number% - }%title%",
                "%track_artist% (%album%) - {%track_number% - }%title%",
                "%title%"
            };

            AddConversion ("track_artist", Catalog.GetString ("Track Artist"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayArtistName);
            });

            AddConversion ("album_artist", Catalog.GetString ("Album Artist"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayAlbumArtistName);
            });

            // Alias for %album_artist%
            AddConversion ("artist", Catalog.GetString ("Album Artist"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayAlbumArtistName);
            });

            AddConversion ("album_artist_initial", Catalog.GetString("Album Artist Initial"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayAlbumArtistName.Substring(0, 1));
            });

            AddConversion ("conductor", Catalog.GetString ("Conductor"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.Conductor);
            });

            AddConversion ("composer", Catalog.GetString ("Composer"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.Composer);
            });

            AddConversion ("genre", Catalog.GetString ("Genre"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayGenre);
            });

            AddConversion ("album", Catalog.GetString ("Album"),
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
    }
}
