//
// AudiobookLibrarySource.cs
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

namespace Banshee.Audiobook
{
    public class AudiobookLibrarySource : LibrarySource
    {
        public AudiobookLibrarySource () : base (Catalog.GetString ("Audiobooks, etc"), "AudiobookLibrary", 49)
        {
            MediaTypes = TrackMediaAttributes.AudioBook;
            NotMediaTypes = TrackMediaAttributes.Podcast | TrackMediaAttributes.VideoStream | TrackMediaAttributes.Music;

            // FIXME
            Properties.SetStringList ("Icon.Name", "document", "source-library");
            Properties.SetString ("TrackView.ColumnControllerXml", String.Format (@"
                <column-controller>
                  <add-all-defaults />
                  <remove-default column=""DiscColumn"" />
                  <remove-default column=""AlbumColumn"" />
                  <remove-default column=""ComposerColumn"" />
                  <remove-default column=""AlbumArtistColumn"" />
                  <remove-default column=""ConductorColumn"" />
                  <remove-default column=""ComposerColumn"" />
                  <remove-default column=""BpmColumn"" />
                  <sort-column direction=""asc"">track_title</sort-column>
                  <column modify-default=""ArtistColumn"">
                    <title>{0}</title>
                    <long-title>{0}</long-title>
                  </column>
                </column-controller>
            ", Catalog.GetString ("Author")));

            var pattern = new AudiobookFileNamePattern ();
            pattern.FolderSchema = CreateSchema<string> ("folder_pattern", pattern.DefaultFolder, "", "");
            pattern.FileSchema   = CreateSchema<string> ("file_pattern",   pattern.DefaultFile, "", "");

            SetFileNamePattern (pattern);
        }

        public override string DefaultBaseDirectory {
            get { return Banshee.Base.XdgBaseDirectorySpec.GetUserDirectory ("XDG_AUDIOBOOKS_DIR", "Audiobooks"); }
        }

        public override bool ShowBrowser {
            get { return false; }
        }

        protected override bool HasArtistAlbum {
            get { return false; }
        }

        public override IEnumerable<SmartPlaylistDefinition> DefaultSmartPlaylists {
            get { yield break; }
        }
    }
}
