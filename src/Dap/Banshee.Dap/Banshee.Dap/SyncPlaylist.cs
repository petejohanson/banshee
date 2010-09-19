// 
// SyncPlaylist.cs
// 
// Author:
//   Andrés G. Aragoneses <knocte@gmail.com>
// 
// Copyright (c) 2010 Andrés G. Aragoneses
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
using Banshee.Playlist;
using Banshee.Sources;

namespace Banshee.Dap
{
    public class SyncPlaylist : PlaylistSource
    {
        public SyncPlaylist (string name, PrimarySource parent, DapLibrarySync libsync) : base (name, parent)
        {
            this.libsync = libsync;
        }

        private DapLibrarySync libsync;

        public override bool HasEditableTrackProperties {
            get {
                // we don't want the user editing the target of a sync, but the origin
                return !libsync.Enabled;
                // NOTE: we could have implemented this simply as "return false" because
                // when switching from AutoSync to ManualSync the playlists are nuked,
                // but it's not clear if it's the intended behavior! (check out BGO#626113)
            }
        }
    }
}
