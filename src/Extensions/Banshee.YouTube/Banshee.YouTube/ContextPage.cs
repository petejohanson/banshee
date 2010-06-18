//
// ContextPage.cs
//
// Author:
//   Kevin Duffus <KevinDuffus@gmail.com>
//
// Copyright (C) 2009 Kevin Duffus
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
using Mono.Unix;
using Gtk;

using Banshee.ContextPane;

using Hyena;

namespace Banshee.YouTube
{
    public class ContextPage : BaseContextPage
    {
        public ContextPage ()
        {
            Id = "YouTube";
            Name = Catalog.GetString ("YouTube");
            IconNames = new string[] { "youtube" };
        }

        private YouTubePane yt_videos;
        public override Widget Widget {
            get { return yt_videos ?? (yt_videos = new YouTubePane (this)); }
        }

        internal void SetState (ContextState state)
        {
            State = state;
        }

        public override void SetTrack (Banshee.Collection.TrackInfo track)
        {
            // Prevent the reloading of the context pane when we play a video
            if (!track.Uri.AbsoluteUri.Contains ("youtube.com")) {
                yt_videos.Query = track.TrackTitle + " by " + track.ArtistName;
            }
        }
    }
}