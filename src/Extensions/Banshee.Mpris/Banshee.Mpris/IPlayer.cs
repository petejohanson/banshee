//
// IPlayer.cs
//
// Author:
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
//
// Copyright (C) 2010 Bertrand Lorentz.
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

namespace Banshee.Mpris
{
    public delegate void DBusPlayerSeekedHandler (long position);

    [Interface ("org.mpris.MediaPlayer2.Player")]
    public interface IPlayer
    {
        event DBusPlayerSeekedHandler Seeked;

        bool CanControl { get; }
        bool CanGoNext { get; }
        bool CanGoPrevious { get; }
        bool CanPause { get; }
        bool CanPlay { get; }
        bool CanSeek { get; }
        double MinimumRate { get; }
        double MaximumRate { get; }
        double Rate { get; set; }
        bool Shuffle { get; set; }
        string LoopStatus { get; set; }
        string PlaybackStatus { get; }
        IDictionary<string, object> Metadata { get; }
        double Volume { get; set; }
        long Position { get; }

        void Next ();
        void Previous ();
        void Pause ();
        void PlayPause ();
        void Stop ();
        void Play ();
        void Seek (long offset);
        void SetPosition (string trackid, long position);
        void OpenUri (string uri);
    }
}

