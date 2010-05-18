//
// BookPlaylist.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (c) 2010 Novell, Inc.
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

using Banshee.Sources;
using Banshee.SmartPlaylist;
using Banshee.Query;
using Banshee.Collection.Database;

namespace Banshee.Audiobook
{
    public class BookPlaylist : SmartPlaylistSource
    {
        AudiobookLibrarySource library;

        public BookPlaylist (string name, AudiobookLibrarySource parent) : base (name, parent)
        {
            library = parent;
            IsTemporary = true;
            Save ();
        }

        DatabaseAlbumInfo book;
        public DatabaseAlbumInfo Book {
            get { return book; }
            set {
                if (value == null) {
                    book = null;
                    return;
                }

                if (book != null && value.DbId == book.DbId) {
                    Reload ();
                } else {
                    book = value;
                    ConditionSql = String.Format ("CoreTracks.AlbumID = {0}", value.DbId);
                    RefreshAndReload ();
                }

                library.Actions.UpdateActions ();
            }
        }

        public override bool CanShuffle {
            get { return false; }
        }
    }
}
