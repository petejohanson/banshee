// 
// AppleDeviceTrackInfo.cs
// 
// Author:
//   Alan McGovern <amcgovern@novell.com>
// 
// Copyright (c) 2010 Moonlight Team
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

using Banshee.Base;
using Banshee.Streaming;
using Banshee.Collection;
using Banshee.Collection.Database;

using Hyena;

namespace Banshee.Dap.AppleDevice
{
    public class AppleDeviceTrackInfo : DatabaseTrackInfo
    {
        internal GPod.Track IpodTrack {
            get; set;
        }

        // Used for podcasts only
        //private string description;

        public AppleDeviceTrackInfo (GPod.Track track)
        {
            IpodTrack = track;
            LoadFromIpodTrack ();
            CanSaveToDatabase = true;
        }

        public AppleDeviceTrackInfo (TrackInfo track)
        {
            if (track is AppleDeviceTrackInfo) {
                IpodTrack = ((AppleDeviceTrackInfo)track).IpodTrack;
                LoadFromIpodTrack ();
            } else {
                AlbumArtist = track.AlbumArtist;
                AlbumTitle = track.AlbumTitle;
                ArtistName = track.ArtistName;
                BitRate = track.BitRate;
                SampleRate = track.SampleRate;
                Bpm = track.Bpm;
                Comment = track.Comment;
                Composer = track.Composer;
                Conductor = track.Conductor;
                Copyright = track.Copyright;
                DateAdded = track.DateAdded;
                DiscCount = track.DiscCount;
                DiscNumber = track.DiscNumber;
                Duration = track.Duration;
                FileSize = track.FileSize;
                Genre = track.Genre;
                Grouping = track.Grouping;
                IsCompilation = track.IsCompilation ;
                LastPlayed = track.LastPlayed;
                LastSkipped = track.LastSkipped;
                PlayCount = track.PlayCount;
                Rating = track.Rating;
                ReleaseDate = track.ReleaseDate;
                SkipCount = track.SkipCount;
                TrackCount = track.TrackCount;
                TrackNumber = track.TrackNumber;
                TrackTitle = track.TrackTitle;
                Year = track.Year;
                MediaAttributes = track.MediaAttributes;

                var podcast_info = track.ExternalObject as IPodcastInfo;
                if (podcast_info != null) {
                    //description = podcast_info.Description;
                    ReleaseDate = podcast_info.ReleaseDate;
                }
            }

            CanSaveToDatabase = true;
        }

        private void LoadFromIpodTrack ()
        {
            var track = IpodTrack;
            try {
                Uri = new SafeUri (System.IO.Path.Combine (track.ITDB.Mountpoint, track.IpodPath.Replace (":", System.IO.Path.DirectorySeparatorChar.ToString ()).Substring (1)));
            } catch (Exception ex) {
                Console.WriteLine (ex);
                Uri = null;
            }

            ExternalId = (long) track.DBID;

            AlbumArtist = track.AlbumArtist;
            AlbumTitle = String.IsNullOrEmpty (track.Album) ? null : track.Album;
            ArtistName = String.IsNullOrEmpty (track.Artist) ? null : track.Artist;
            BitRate = track.Bitrate;
            SampleRate = track.Samplerate;
            Bpm = (int)track.BPM;
            Comment = track.Comment;
            Composer = track.Composer;
            DateAdded = track.TimeAdded;
            DiscCount = track.CDs;
            DiscNumber = track.CDNumber;
            Duration = TimeSpan.FromMilliseconds (track.TrackLength);
            FileSize = track.Size;
            Genre = String.IsNullOrEmpty (track.Genre) ? null : track.Genre;
            Grouping = track.Grouping;
            IsCompilation = track.Compilation;
            LastPlayed = track.TimePlayed;
            PlayCount = (int) track.PlayCount;
            TrackCount = track.Tracks;
            TrackNumber = track.TrackNumber;
            TrackTitle = String.IsNullOrEmpty (track.Title) ? null : track.Title;
            Year = track.Year;
            //description = track.Description;
            ReleaseDate = track.TimeReleased;

            rating = track.Rating > 5 ? 0 : (int) track.Rating;

            if (track.DRMUserID > 0) {
                PlaybackError = StreamPlaybackError.Drm;
            }

            MediaAttributes = TrackMediaAttributes.AudioStream | TrackMediaAttributes.Music;

//            switch (track.Type) {
//                case IPod.MediaType.Audio:
//                    MediaAttributes |= TrackMediaAttributes.Music;
//                    break;
//                case IPod.MediaType.AudioVideo:
//                case IPod.MediaType.Video:
//                    MediaAttributes |= TrackMediaAttributes.VideoStream;
//                    break;
//                case IPod.MediaType.MusicVideo:
//                    MediaAttributes |= TrackMediaAttributes.Music | TrackMediaAttributes.VideoStream;
//                    break;
//                case IPod.MediaType.Movie:
//                    MediaAttributes |= TrackMediaAttributes.VideoStream | TrackMediaAttributes.Movie;
//                    break;
//                case IPod.MediaType.TVShow:
//                    MediaAttributes |= TrackMediaAttributes.VideoStream | TrackMediaAttributes.TvShow;
//                    break;
//                case IPod.MediaType.VideoPodcast:
//                    MediaAttributes |= TrackMediaAttributes.VideoStream | TrackMediaAttributes.Podcast;
//                    break;
//                case IPod.MediaType.Podcast:
//                    MediaAttributes |= TrackMediaAttributes.Podcast;
//                    // FIXME: persist URL on the track (track.PodcastUrl)
//                    break;
//                case IPod.MediaType.Audiobook:
//                    MediaAttributes |= TrackMediaAttributes.AudioBook;
//                    break;
//            }
        }

        public void CommitToIpod (GPod.ITDB database)
        {
            bool addTrack = IpodTrack == null;
            Console.WriteLine ("Committing a track to {0}", database);
            if (IpodTrack == null) {
                IpodTrack = new GPod.Track ();
            }

            var track = IpodTrack;
            track.AlbumArtist = AlbumArtist;
            track.Bitrate = BitRate;
            track.Samplerate= (ushort)SampleRate;
            track.BPM = (short)Bpm;
            track.Comment = Comment;
            track.Composer = Composer;
            track.TimeAdded = DateAdded;
            track.CDs = DiscCount;
            track.CDNumber = DiscNumber;
            track.TrackLength = (int) Duration.TotalMilliseconds;
            track.Size = (int)FileSize;
            track.Grouping = Grouping;
            track.Compilation = IsCompilation;
            track.TimePlayed = LastPlayed;
            track.PlayCount = (uint) PlayCount;
            track.Tracks = TrackCount;
            track.TrackNumber = TrackNumber;
            track.Year = Year;
            track.TimeReleased = ReleaseDate;

            track.Album = AlbumTitle;
            track.Artist = ArtistName;
            track.Title = TrackTitle;
            track.Genre = Genre;

            switch (Rating) {
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
                track.Rating = (uint) rating;
                break;
            default: track.Rating = 0;
                break;
            }

//            if (HasAttribute (TrackMediaAttributes.Podcast)) {
//                track.DateReleased = ReleaseDate;
//                track.Description = description;
//                track.RememberPosition = true;
//                track.NotPlayedMark = track.PlayCount == 0;
//            }
//
//            if (HasAttribute (TrackMediaAttributes.VideoStream)) {
//                if (HasAttribute (TrackMediaAttributes.Podcast)) {
//                    track.Type = IPod.MediaType.VideoPodcast;
//                } else if (HasAttribute (TrackMediaAttributes.Music)) {
//                    track.Type = IPod.MediaType.MusicVideo;
//                } else if (HasAttribute (TrackMediaAttributes.Movie)) {
//                    track.Type = IPod.MediaType.Movie;
//                } else if (HasAttribute (TrackMediaAttributes.TvShow)) {
//                    track.Type = IPod.MediaType.TVShow;
//                } else {
//                    track.Type = IPod.MediaType.Video;
//                }
//            } else {
//                if (HasAttribute (TrackMediaAttributes.Podcast)) {
//                    track.Type = IPod.MediaType.Podcast;
//                } else if (HasAttribute (TrackMediaAttributes.AudioBook)) {
//                    track.Type = IPod.MediaType.Audiobook;
//                } else if (HasAttribute (TrackMediaAttributes.Music)) {
//                    track.Type = IPod.MediaType.Audio;
//                } else {
//                    track.Type = IPod.MediaType.Audio;
//                }
//            }
            track.MediaType = GPod.MediaType.Audio;
            if (addTrack) {
                track.Filetype = "MP3-file";
                database.Tracks.Add (IpodTrack);
                database.MasterPlaylist.Tracks.Add (IpodTrack);
                database.CopyTrackToIPod (track, Uri.LocalPath);
                ExternalId = (long) IpodTrack.DBID;
                Console.WriteLine ("The ipod path is now: {0}", IpodTrack.IpodPath);
            }
//            if (CoverArtSpec.CoverExists (ArtworkId)) {
//                SetIpodCoverArt (device, track, CoverArtSpec.GetPath (ArtworkId));
//            }
        }

        // FIXME: No reason for this to use GdkPixbuf - the file is on disk already in
        // the artwork cache as a JPEG, so just shove the bytes from disk into the track
        public static void SetIpodCoverArt (GPod.Device device, GPod.Track track, string path)
        {
//            try {
//                Gdk.Pixbuf pixbuf = null;
//                foreach (IPod.ArtworkFormat format in device.LookupArtworkFormats (IPod.ArtworkUsage.Cover)) {
//                    if (!track.HasCoverArt (format)) {
//                        // Lazily load the pixbuf
//                        if (pixbuf == null) {
//                            pixbuf = new Gdk.Pixbuf (path);
//                        }
//
//                        track.SetCoverArt (format, IPod.ArtworkHelpers.ToBytes (format, pixbuf));
//                    }
//                }
//
//                if (pixbuf != null) {
//                    pixbuf.Dispose ();
//                }
//            } catch (Exception e) {
//                Log.Exception (String.Format ("Failed to set cover art on iPod from {0}", path), e);
//            }
        }
    }
}
