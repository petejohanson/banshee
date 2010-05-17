//
// AudiobookModel.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008-2009 Novell, Inc.
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
using System.Collections.Generic;

using Mono.Unix;

using Banshee.Library;
using Banshee.Collection;
using Banshee.SmartPlaylist;
using Banshee.Collection.Database;
using Banshee.Sources;
using Banshee.Database;
using Banshee.ServiceStack;

using Banshee.Sources.Gui;

namespace Banshee.Audiobook
{
    public class AudiobookModel : DatabaseAlbumListModel
    {
        public AudiobookModel (AudiobookLibrarySource source, DatabaseTrackListModel trackModel, BansheeDbConnection connection, string uuid) : base (source, trackModel, connection, uuid)
        {
            Selection = new Hyena.Collections.Selection ();
            HasSelectAllItem = false;

            ReloadFragmentFormat = String.Format (@"
                FROM CoreAlbums WHERE CoreAlbums.AlbumID IN (SELECT AlbumID FROM CoreTracks WHERE PrimarySourceID = {0})
                ORDER BY CoreAlbums.TitleSortKey, CoreAlbums.ArtistNameSortKey",
                source.DbId);
        }
    }
}
