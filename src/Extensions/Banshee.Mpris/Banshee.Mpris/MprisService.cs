//
// Service.cs
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
using org.freedesktop.DBus;
using NDesk.DBus;
using Hyena;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.ServiceStack;

namespace Banshee.Mpris
{
    public class MprisService : IExtensionService, IDisposable
    {
        private static string bus_name = "org.mpris.MediaPlayer2.banshee";

        private MediaPlayer player;

        public MprisService ()
        {
        }

        void IExtensionService.Initialize ()
        {
            if (!DBusConnection.Enabled) {
                return;
            }

            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent,
                PlayerEvent.StartOfStream |
                PlayerEvent.StateChange |
                PlayerEvent.TrackInfoUpdated |
                PlayerEvent.Seek |
                PlayerEvent.Volume);
            ServiceManager.PlaybackController.RepeatModeChanged += OnRepeatModeChanged;
            ServiceManager.PlaybackController.ShuffleModeChanged += OnShuffleModeChanged;

            player = new MediaPlayer();
            Bus.Session.Register (MediaPlayer.Path, player);

            if (Bus.Session.RequestName (bus_name) != RequestNameReply.PrimaryOwner) {
                Hyena.Log.Warning ("MPRIS service couldn't grab bus name");
                return;
            }
        }

        void IDisposable.Dispose ()
        {
            Bus.Session.Unregister (MediaPlayer.Path);

            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);
            ServiceManager.PlaybackController.RepeatModeChanged -= OnRepeatModeChanged;
            ServiceManager.PlaybackController.ShuffleModeChanged -= OnShuffleModeChanged;

            Bus.Session.ReleaseName (bus_name);
        }

        private void OnPlayerEvent (PlayerEventArgs args)
        {
            switch (args.Event) {
                case PlayerEvent.StartOfStream:
                case PlayerEvent.TrackInfoUpdated:
                    player.AddPropertyChange (PlayerProperties.Metadata);
                    break;
                case PlayerEvent.StateChange:
                    player.AddPropertyChange (PlayerProperties.PlaybackStatus);
                    break;
                case PlayerEvent.Seek:
                    player.HandleSeek ();
                    break;
                case PlayerEvent.Volume:
                    player.AddPropertyChange (PlayerProperties.Volume);
                    break;
            }
        }

        private void OnRepeatModeChanged (object o, EventArgs<PlaybackRepeatMode> args)
        {
            player.AddPropertyChange (PlayerProperties.LoopStatus);
        }

        private void OnShuffleModeChanged (object o, EventArgs<string> args)
        {
            player.AddPropertyChange (PlayerProperties.Shuffle);
        }

        string IService.ServiceName {
            get { return "MprisService"; }
        }
    }
}
