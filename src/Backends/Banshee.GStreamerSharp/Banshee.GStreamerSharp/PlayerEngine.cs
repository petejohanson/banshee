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

        public PlayerEngine ()
        {
            Console.WriteLine ("Gst# PlayerEngine ctor - completely experimental, still a WIP");
            Gst.Application.Init ();
            pipeline = new Pipeline ();
            playbin = new PlayBin2 ();
            pipeline.Add (playbin);

            pipeline.Bus.AddWatch (OnBusMessage);

            Banshee.ServiceStack.Application.RunTimeout (200, delegate {
                OnEventChanged (PlayerEvent.Iterate);
                return true;
            });

            OnStateChanged (PlayerState.Ready);
        }

        private bool OnBusMessage (Bus bus, Message msg)
        {
            switch (msg.Type) {
                case MessageType.Eos:
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

                    // TODO: What to do with the error?

                    break;
            }

            return true;
        }

        private void HandleBuffering (int buffer_percent)
        {
            OnEventChanged (new PlayerEventBufferingArgs (buffer_percent / 100.0));
        }

        private void HandleStateChange (State old_state, State new_state, State pending_state)
        {
            if (CurrentState != PlayerState.Loaded && old_state == State.Ready && new_state == State.Paused && pending_state == State.Playing) {
                OnStateChanged (PlayerState.Loaded);
            } else if (old_state == State.Paused && new_state == State.Playing && pending_state == State.VoidPending) {
                if (CurrentState == PlayerState.Loaded) {
                    OnEventChanged (PlayerEvent.StartOfStream);
                }
                OnStateChanged (PlayerState.Playing);
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

        protected override void OpenUri (SafeUri uri)
        {
            Console.WriteLine ("Gst# PlayerEngine OpenUri: {0}", uri);
            if (pipeline.CurrentState == State.Playing) {
                pipeline.SetState (Gst.State.Null);
            }
            playbin.Uri = uri.AbsoluteUri;
        }

        public override void Play ()
        {
            Console.WriteLine ("Gst# PlayerEngine play");
            pipeline.SetState (Gst.State.Playing);
            OnStateChanged (PlayerState.Playing);
        }

        public override void Pause ()
        {
            Console.WriteLine ("Gst# PlayerEngine pause");
            pipeline.SetState (Gst.State.Paused);
            OnStateChanged (PlayerState.Paused);
        }

        public override string GetSubtitleDescription (int index)
        {
            return playbin.GetTextTags (index)
             .GetTag (Gst.Tag.LanguageCode)
             .FirstOrDefault (t => t != null);
        }

        public override ushort Volume {
            get { return (ushort) Math.Round (playbin.Volume * 100.0); }
            set { playbin.Volume = (value / 100.0); }
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
            get { return playbin.Suburi; }
        }
    }
}
