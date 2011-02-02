//
// IPlaylists.cs
//
// Author:
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
//
// Copyright 2010 Bertrand Lorentz
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
using DBus;

namespace Banshee.Mpris
{
    public delegate void PlaylistChangedHandler (Playlist playlist);

    public struct Playlist
    {
        public ObjectPath Id;
        public string Name;
        public string Icon;
    }

    public struct MaybePlaylist
    {
        public bool Valid;
        public Playlist Playlist;
    }

    [Interface ("org.mpris.MediaPlayer2.Playlists")]
    public interface IPlaylists
    {
        event PlaylistChangedHandler PlaylistChanged;

        uint PlaylistCount { get; }
        string [] Orderings { get; }
        MaybePlaylist ActivePlaylist { get; }

        void ActivatePlaylist (ObjectPath playlist_id);
        Playlist [] GetPlaylists (uint index, uint max_count, string order, bool reverse_order);
    }
}

