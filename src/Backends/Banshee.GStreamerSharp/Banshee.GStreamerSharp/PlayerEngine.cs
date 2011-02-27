//
// PlayerEngine.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
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
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

using Mono.Unix;

using Gst;
using Gst.BasePlugins;

using Hyena;
using Hyena.Data;

using Banshee.Base;
using Banshee.Streaming;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Preferences;

namespace Banshee.GStreamerSharp
{
    public class PlayerEngine : Banshee.MediaEngine.PlayerEngine
    {
        Pipeline pipeline;
        PlayBin2 playbin;
        uint iterate_timeout_id = 0;

        public PlayerEngine ()
        {
            Log.InformationFormat ("GStreamer# {0} Initializing; {1}.{2}",
                typeof (Gst.Version).Assembly.GetName ().Version, Gst.Version.Description, Gst.Version.Nano);

            // Setup the gst plugins/registry paths if running Windows
            if (PlatformDetection.IsWindows) {
                var gst_paths = new string [] { Hyena.Paths.Combine (Hyena.Paths.InstalledApplicationPrefix, "gst-plugins") };
                Environment.SetEnvironmentVariable ("GST_PLUGIN_PATH", String.Join (";", gst_paths));
                Environment.SetEnvironmentVariable ("GST_PLUGIN_SYSTEM_PATH", "");
                Environment.SetEnvironmentVariable ("GST_DEBUG", "1");

                string registry = Hyena.Paths.Combine (Hyena.Paths.ApplicationData, "registry.bin");
                if (!System.IO.File.Exists (registry)) {
                    System.IO.File.Create (registry).Close ();
                }

                Environment.SetEnvironmentVariable ("GST_REGISTRY", registry);

                //System.Environment.SetEnvironmentVariable ("GST_REGISTRY_FORK", "no");
                Log.DebugFormat ("GST_PLUGIN_PATH = {0}", Environment.GetEnvironmentVariable ("GST_PLUGIN_PATH"));
            }

            Gst.Application.Init ();
            pipeline = new Pipeline ();
            playbin = new PlayBin2 ();
            pipeline.Add (playbin);

            // Remember the volume from last time
            Volume = (ushort)PlayerEngineService.VolumeSchema.Get ();

            playbin.AddNotification ("volume", OnVolumeChanged);
            pipeline.Bus.AddWatch (OnBusMessage);

            OnStateChanged (PlayerState.Ready);
        }

        private bool OnBusMessage (Bus bus, Message msg)
        {
            switch (msg.Type) {
                case MessageType.Eos:
                    StopIterating ();
                    Close (false);
                    OnEventChanged (PlayerEvent.EndOfStream);
                    OnEventChanged (PlayerEvent.RequestNextTrack);
                    break;

                case MessageType.StateChanged:
                    State old_state, new_state, pending_state;
                    msg.ParseStateChanged (out old_state, out new_state, out pending_state);
                    HandleStateChange (old_state, new_state, pending_state);
                    break;

                case MessageType.Buffering:
                    int buffer_percent;
                    msg.ParseBuffering (out buffer_percent);
                    HandleBuffering (buffer_percent);
                    break;

                case MessageType.Tag:
                    Pad pad;
                    TagList tag_list;
                    msg.ParseTag (out pad, out tag_list);

                    HandleTag (pad, tag_list);
                    break;

                case MessageType.Error:
                    Enum error_type;
                    string err_msg, debug;
                    msg.ParseError (out error_type, out err_msg, out debug);

                    HandleError (error_type, err_msg, debug);

                    break;
            }

            return true;
        }

        private void OnVolumeChanged (object o, Gst.GLib.NotifyArgs args)
        {
            OnEventChanged (PlayerEvent.Volume);
        }

        private void HandleError (Enum domain, string error_message, string debug)
        {
            Close (true);

            error_message = error_message ?? Catalog.GetString ("Unknown Error");

            if (domain is ResourceError) {
                ResourceError domain_code = (ResourceError)domain;
                if (CurrentTrack != null) {
                    switch (domain_code) {
                    case ResourceError.NotFound:
                        CurrentTrack.SavePlaybackError (StreamPlaybackError.ResourceNotFound);
                        break;
                    default:
                        break;
                    }
                }
                Log.Error (String.Format ("GStreamer resource error: {0}", domain_code), false);
            } else if (domain is StreamError) {
                StreamError domain_code = (StreamError)domain;
                if (CurrentTrack != null) {
                    switch (domain_code) {
                    case StreamError.CodecNotFound:
                        CurrentTrack.SavePlaybackError (StreamPlaybackError.CodecNotFound);
                        break;
                    default:
                        break;
                    }
                }

                Log.Error (String.Format ("GStreamer stream error: {0}", domain_code), false);
            } else if (domain is CoreError) {
                CoreError domain_code = (CoreError)domain;
                if (CurrentTrack != null) {
                    switch (domain_code) {
                    case CoreError.MissingPlugin:
                        CurrentTrack.SavePlaybackError (StreamPlaybackError.CodecNotFound);
                        break;
                    default:
                        break;
                    }
                }

                if (domain_code != CoreError.MissingPlugin) {
                    Log.Error (String.Format ("GStreamer core error: {0}", (CoreError)domain), false);
                }
            } else if (domain is LibraryError) {
                Log.Error (String.Format ("GStreamer library error: {0}", (LibraryError)domain), false);
            }

            OnEventChanged (new PlayerEventErrorArgs (error_message));
        }

        private void HandleBuffering (int buffer_percent)
        {
            OnEventChanged (new PlayerEventBufferingArgs (buffer_percent / 100.0));
        }

        private void HandleStateChange (State old_state, State new_state, State pending_state)
        {
            StopIterating ();
            if (CurrentState != PlayerState.Loaded && old_state == State.Ready && new_state == State.Paused && pending_state == State.Playing) {
                OnStateChanged (PlayerState.Loaded);
            } else if (old_state == State.Paused && new_state == State.Playing && pending_state == State.VoidPending) {
                if (CurrentState == PlayerState.Loaded) {
                    OnEventChanged (PlayerEvent.StartOfStream);
                }
                OnStateChanged (PlayerState.Playing);
                StartIterating ();
            } else if (CurrentState == PlayerState.Playing && old_state == State.Playing && new_state == State.Paused) {
                OnStateChanged (PlayerState.Paused);
            }
        }

        private void HandleTag (Pad pad, TagList tag_list)
        {
            foreach (string tag in tag_list.Tags) {
                if (String.IsNullOrEmpty (tag)) {
                    continue;
                }

                if (tag_list.GetTagSize (tag) < 1) {
                    continue;
                }

                List tags = tag_list.GetTag (tag);

                foreach (object o in tags) {
                    OnTagFound (new StreamTag () { Name = tag, Value = o });
                }
            }
        }

        private bool OnIterate ()
        {
            // Actual iteration.
            OnEventChanged (PlayerEvent.Iterate);
            // Run forever until we are stopped
            return true;
        }

        private void StartIterating ()
        {
            if (iterate_timeout_id > 0) {
                GLib.Source.Remove (iterate_timeout_id);
                iterate_timeout_id = 0;
            }

            iterate_timeout_id = GLib.Timeout.Add (200, OnIterate);
        }

        private void StopIterating ()
        {
            if (iterate_timeout_id > 0) {
                GLib.Source.Remove (iterate_timeout_id);
                iterate_timeout_id = 0;
            }
        }

        protected override void OpenUri (SafeUri uri, bool maybeVideo)
        {
            if (pipeline.CurrentState == State.Playing || pipeline.CurrentState == State.Paused) {
                pipeline.SetState (Gst.State.Ready);
            }

            playbin.Uri = uri.AbsoluteUri;
        }

        public override void Play ()
        {
            // HACK, I think that directsoundsink has a bug that resets its volume to 1.0 every time
            // This seems to fix bgo#641427
            Volume = Volume;
            pipeline.SetState (Gst.State.Playing);
            OnStateChanged (PlayerState.Playing);
        }

        public override void Pause ()
        {
            pipeline.SetState (Gst.State.Paused);
            OnStateChanged (PlayerState.Paused);
        }

        public override void Close (bool fullShutdown)
        {
            pipeline.SetState (State.Null);
            base.Close (fullShutdown);
        }

        public override string GetSubtitleDescription (int index)
        {
            return playbin.GetTextTags (index)
             .GetTag (Gst.Tag.LanguageCode)
             .Cast<string> ()
             .FirstOrDefault (t => t != null);
        }

        public override ushort Volume {
            get { return (ushort) Math.Round (playbin.Volume * 100.0); }
            set {
                double volume = Math.Min (1.0, Math.Max (0, value / 100.0));
                playbin.Volume = volume;
                PlayerEngineService.VolumeSchema.Set (value);
            }
        }

        public override bool CanSeek {
            get { return true; }
        }

        private static Format query_format = Format.Time;
        public override uint Position {
            get {
                long pos;
                playbin.QueryPosition (ref query_format, out pos);
                return (uint) ((ulong)pos / Gst.Clock.MSecond);
            }
            set {
                playbin.Seek (Format.Time, SeekFlags.Accurate, (long)(value * Gst.Clock.MSecond));
            }
        }

        public override uint Length {
            get {
                long duration;
                playbin.QueryDuration (ref query_format, out duration);
                return (uint) ((ulong)duration / Gst.Clock.MSecond);
            }
        }

        private static string [] source_capabilities = { "file", "http", "cdda" };
        public override IEnumerable SourceCapabilities {
            get { return source_capabilities; }
        }

        private static string [] decoder_capabilities = { "ogg", "wma", "asf", "flac", "mp3", "" };
        public override IEnumerable ExplicitDecoderCapabilities {
            get { return decoder_capabilities; }
        }

        public override string Id {
            get { return "gstreamer-sharp"; }
        }

        public override string Name {
            get { return Catalog.GetString ("GStreamer# 0.10"); }
        }

        public override bool SupportsEqualizer {
            get { return false; }
        }

        public override VideoDisplayContextType VideoDisplayContextType {
            get { return VideoDisplayContextType.Unsupported; }
        }

        public override int SubtitleCount {
            get { return playbin.NText; }
        }

        public override int SubtitleIndex {
            set {
                if (value >= 0 && value < SubtitleCount) {
                    playbin.CurrentText = value;
                }
            }
        }

        public override SafeUri SubtitleUri {
            set { playbin.Suburi = value.AbsoluteUri; }
            get { return new SafeUri (playbin.Suburi); }
        }
    }
}
