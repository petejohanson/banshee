//
// MediaPlayer.cs
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

using NDesk.DBus;
using Hyena;

using Banshee.Gui;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.ServiceStack;

namespace Banshee.Mpris
{
    public class MediaPlayer : IMediaPlayer, IPlayer, IProperties
    {
        private static string mediaplayer_interface_name = "org.mpris.MediaPlayer2";
        private static string player_interface_name = "org.mpris.MediaPlayer2.Player";
        private PlaybackControllerService playback_service;
        private PlayerEngineService engine_service;
        private Dictionary<string, object> changed_properties;
        private List<string> invalidated_properties;

        private static ObjectPath path = new ObjectPath ("/org/mpris/MediaPlayer2");
        public static ObjectPath Path {
            get { return path; }
        }

        private event PropertiesChangedHandler properties_changed;
        event PropertiesChangedHandler IProperties.PropertiesChanged {
            add { properties_changed += value; }
            remove { properties_changed -= value; }
        }

        private event DBusPlayerSeekedHandler dbus_seeked;
        event DBusPlayerSeekedHandler IPlayer.Seeked {
            add { dbus_seeked += value; }
            remove { dbus_seeked -= value; }
        }

        public MediaPlayer ()
        {
            playback_service = ServiceManager.PlaybackController;
            engine_service = ServiceManager.PlayerEngine;
            changed_properties = new Dictionary<string, object> ();
            invalidated_properties = new List<string> ();
        }

#region IMediaPlayer

        public bool CanQuit {
            get { return true; }
        }

        public bool CanRaise {
            get { return true; }
        }

        public bool HasTrackList {
            get { return false; }
        }

        public string Identity {
            get { return "Banshee"; }
        }

        public string DesktopEntry {
            get { return "banshee-1"; }
        }

        // This is just a list of commonly supported MIME types.
        // We don't know exactly which ones are supported by the PlayerEngine
        private static string [] supported_mimetypes = { "application/ogg", "audio/flac",
            "audio/mp3", "audio/mp4", "audio/mpeg", "audio/ogg", "audio/vorbis", "audio/wav",
            "audio/x-flac", "audio/x-vorbis+ogg",
            "video/avi", "video/mp4", "video/mpeg" };
        public string [] SupportedMimeTypes {
            get { return supported_mimetypes; }
        }

        public string [] SupportedUriSchemes {
            get { return (string [])ServiceManager.PlayerEngine.ActiveEngine.SourceCapabilities; }
        }

        public void Raise ()
        {
            if (!CanRaise) {
                return;
            }

            ServiceManager.Get<GtkElementsService> ().PrimaryWindow.SetVisible (true);
        }

        public void Quit ()
        {
            if (!CanQuit) {
                return;
            }

            Application.Shutdown ();
        }

#endregion

#region IPlayer

        public bool CanControl {
            get { return true; }
        }

        // We don't really know if we can actually go next or previous
        public bool CanGoNext {
            get { return CanControl; }
        }

        public bool CanGoPrevious {
            get { return CanControl; }
        }

        public bool CanPause {
            get { return engine_service.CanPause; }
        }

        public bool CanPlay {
            get { return CanControl; }
        }

        public bool CanSeek {
            get { return engine_service.CanSeek; }
        }

        public double MaximumRate {
            get { return 1.0; }
        }

        public double MinimumRate {
            get { return 1.0; }
        }

        public double Rate {
            get { return 1.0; }
            set {}
        }

        public bool Shuffle {
            get { return !(playback_service.ShuffleMode == "off"); }
            set { playback_service.ShuffleMode = value ? "song" : "off"; }
        }

        public string LoopStatus {
            get {
                string loop_status;
                switch (playback_service.RepeatMode) {
                    case PlaybackRepeatMode.None:
                        loop_status = "None";
                        break;
                    case PlaybackRepeatMode.RepeatSingle:
                        loop_status = "Track";
                        break;
                    case PlaybackRepeatMode.RepeatAll:
                        loop_status = "Playlist";
                        break;
                    default:
                        loop_status = "None";
                        break;
                }
                return loop_status;
            }
            set {
                switch (value) {
                    case "None":
                        playback_service.RepeatMode = PlaybackRepeatMode.None;
                        break;
                    case "Track":
                        playback_service.RepeatMode = PlaybackRepeatMode.RepeatSingle;
                        break;
                    case "Playlist":
                        playback_service.RepeatMode = PlaybackRepeatMode.RepeatAll;
                        break;
                }
            }
        }

        public string PlaybackStatus {
            get {
                string status;
                switch (engine_service.CurrentState) {
                    case PlayerState.Playing:
                        status = "Playing";
                        break;
                    case PlayerState.Paused:
                        status = "Paused";
                        break;
                    default:
                        status = "Stopped";
                        break;
                }
                return status;
            }
        }

        public IDictionary<string, object> Metadata {
            get {
                var metadata = new Metadata (playback_service.CurrentTrack);
                return metadata.DataStore;
            }
        }

        public double Volume {
            get { return engine_service.Volume / 100.0; }
            set { engine_service.Volume = (ushort)Math.Round (value * 100); }
        }

        // Position is expected in microseconds
        public long Position {
            get { return (long)engine_service.Position * 1000; }
        }

        public void Next ()
        {
            playback_service.Next ();
        }

        public void Previous ()
        {
            playback_service.Previous ();
        }

        public void Pause ()
        {
            engine_service.Pause ();
        }

        public void PlayPause ()
        {
            engine_service.TogglePlaying ();
        }

        public void Stop ()
        {
            engine_service.Close ();
        }

        public void Play ()
        {
            engine_service.Play ();
        }

        public void SetPosition (string trackid, long position)
        {
            if (!CanSeek) {
                return;
            }

            if (String.IsNullOrEmpty (trackid) || trackid != (string)Metadata["trackid"]) {
                return;
            }

            // position is in microseconds, we speak in milliseconds
            long position_ms = position / 1000;
            if (position_ms < 0 || position_ms > playback_service.CurrentTrack.Duration.TotalMilliseconds) {
                return;
            }

            engine_service.Position = (uint)position_ms;
        }

        public void Seek (long position)
        {
            if (!CanSeek) {
                return;
            }

            // position is in microseconds, relative to the current position and can be negative
            long new_pos = (int)engine_service.Position + (position / 1000);
            if (new_pos < 0) {
                engine_service.Position = 0;
            } else {
                engine_service.Position = (uint)new_pos;
            }
        }

        public void OpenUri (string uri)
        {
            engine_service.Open (new SafeUri (uri));
            engine_service.Play ();
        }

#endregion

#region Signals

        public void HandlePropertiesChange ()
        {
            PropertiesChangedHandler handler = properties_changed;
            if (handler != null) {
                lock (changed_properties) {
                    // Properties that trigger this signal are all on the Player interface
                    handler (player_interface_name, changed_properties, invalidated_properties.ToArray ());
                    changed_properties.Clear ();
                    invalidated_properties.Clear ();
                }
            }
        }

        public void HandleSeek ()
        {
            DBusPlayerSeekedHandler dbus_handler = dbus_seeked;
            if (dbus_handler != null) {
                dbus_handler (Position);
            }
        }

        public void AddPropertyChange (params PlayerProperties [] properties)
        {
            lock (changed_properties) {
                foreach (PlayerProperties prop in properties) {
                    string prop_name = prop.ToString ();
                    changed_properties[prop_name] = Get (player_interface_name, prop_name);
                }
            }
            // TODO We could check if a property really has changed and only fire the event in that case
            HandlePropertiesChange ();
        }

#endregion

#region Dbus.Properties

        private static string [] mediaplayer_properties = { "CanQuit", "CanRaise", "HasTrackList", "Identity",
            "DesktopEntry", "SupportedMimeTypes", "SupportedUriSchemes" };

        private static string [] player_properties = { "CanControl", "CanGoNext", "CanGoPrevious", "CanPause",
            "CanPlay", "CanSeek", "LoopStatus", "MaximumRate", "Metadata", "MinimumRate", "PlaybackStatus",
            "Position", "Rate", "Shuffle", "Volume" };

        public object Get (string interface_name, string propname)
        {
            if (interface_name == mediaplayer_interface_name) {
                switch (propname) {
                    case "CanQuit":
                        return CanQuit;
                    case "CanRaise":
                        return CanRaise;
                    case "HasTrackList":
                        return HasTrackList;
                    case "Identity":
                        return Identity;
                    case "DesktopEntry":
                        return DesktopEntry;
                    case "SupportedMimeTypes":
                        return SupportedMimeTypes;
                    case "SupportedUriSchemes":
                        return SupportedUriSchemes;
                    default:
                        return null;
                }
            } else if (interface_name == player_interface_name) {
                switch (propname) {
                    case "CanControl":
                        return CanControl;
                    case "CanGoNext":
                        return CanGoNext;
                    case "CanGoPrevious":
                        return CanGoPrevious;
                    case "CanPause":
                        return CanPause;
                    case "CanPlay":
                        return CanPlay;
                    case "CanSeek":
                        return CanSeek;
                    case "MinimumRate":
                        return MinimumRate;
                    case "MaximumRate":
                        return MaximumRate;
                    case "Rate":
                        return Rate;
                    case "Shuffle":
                        return Shuffle;
                    case "LoopStatus":
                        return LoopStatus;
                    case "PlaybackStatus":
                        return PlaybackStatus;
                    case "Position":
                        return Position;
                    case "Metadata":
                        return Metadata;
                    case "Volume":
                        return Volume;
                    default:
                        return null;
                }
            } else {
                return null;
            }
        }

        public void Set (string interface_name, string propname, object value)
        {
            // All writable properties are on the Player interface
            if (interface_name != player_interface_name) {
                return;
            }

            switch (propname) {
            case "LoopStatus":
                string s = value as string;
                if (!String.IsNullOrEmpty (s)) {
                    LoopStatus = s;
                }
                break;
            case "Shuffle":
                if (value is bool) {
                    Shuffle = (bool)value;
                }
                break;
            case "Volume":
                if (value is double) {
                    Volume = (double)value;
                }
                break;
            }
        }

        public IDictionary<string, object> GetAll (string interface_name)
        {
            var props = new Dictionary<string, object> ();

            if (interface_name == mediaplayer_interface_name) {
                foreach (string prop in mediaplayer_properties) {
                    props.Add (prop, Get (interface_name, prop));
                }
            } else if (interface_name == player_interface_name) {
                foreach (string prop in player_properties) {
                    props.Add (prop, Get (interface_name, prop));
                }
            }

            return props;
        }
    }

#endregion

    // Those are all the properties that can trigger the PropertiesChanged signal
    // The names must match exactly the names of the properties
    public enum PlayerProperties
    {
        CanControl,
        CanGoNext,
        CanGoPrevious,
        CanPause,
        CanPlay,
        CanSeek,
        MinimumRate,
        MaximumRate,
        Rate,
        Shuffle,
        LoopStatus,
        PlaybackStatus,
        Metadata,
        Volume
    }
}
