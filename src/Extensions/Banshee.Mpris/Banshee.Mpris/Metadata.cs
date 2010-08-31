//
// Metadata.cs
//
// Authors:
//   John Millikin <jmillikin@gmail.com>
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
//
// Copyright (C) 2009 John Millikin
// Copyright (C) 2010 Bertrand Lorentz
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
using Banshee.Base;
using Banshee.Collection;
using Banshee.ServiceStack;

namespace Banshee.Mpris
{
    public class Metadata
    {
        private static string object_path = String.Concat (DBusServiceManager.ObjectRoot, "/Track/");

        private Dictionary<string, object> data_store;

        public Metadata (TrackInfo track)
        {
            data_store = new Dictionary<string, object> ();

            if (track == null) {
                // Managed dbus doesn't like null or empty dictionaries
                data_store["mpris:trackid"] = String.Concat (object_path, "Empty");
                return;
            }

            // The trackid must be formatted like a dbus object path
            data_store["mpris:trackid"] = String.Concat (object_path, track.CacheModelId, track.CacheEntryId);
            SetInfo ("mpris:length", (long)track.Duration.TotalMilliseconds * 1000);
            SetInfo ("xesam:url", track.Uri.ToString ());
            SetInfo ("xesam:title", track.TrackTitle);
            SetInfo ("xesam:album", track.AlbumTitle);
            if (!String.IsNullOrEmpty (track.ArtistName)) {
                SetInfo ("xesam:artist", new string [] {track.ArtistName});
            }
            if (!String.IsNullOrEmpty (track.AlbumArtist)) {
                SetInfo ("xesam:albumArtist", new string [] {track.AlbumArtist});
            }
            if (!String.IsNullOrEmpty (track.Genre)) {
                SetInfo ("xesam:genre", new string [] {track.Genre});
            }
            if (!String.IsNullOrEmpty (track.Comment)) {
                SetInfo ("xesam:comment", new string [] {track.Comment});
            }

            if (track.TrackNumber > 0) {
                data_store["xesam:trackNumber"] = track.TrackNumber;
            }

            if (track.ReleaseDate.Ticks > 0) {
                SetInfo ("xesam:contentCreated", track.ReleaseDate.ToString ("s"));
            }

            string artid = track.ArtworkId;
            if (artid != null) {
                SetInfo ("mpris:artUrl", String.Concat ("file://", CoverArtSpec.GetPath (artid)));
            }
        }

        private void SetInfo (string name, object property)
        {
            if (property == null) {
                return;
            }
            data_store[name] = property;
        }

        public IDictionary<string, object> DataStore {
            get { return data_store; }
        }
    }
}
