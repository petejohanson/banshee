//
// IMediaPlayer.cs
//
// Author:
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
//
// Copyright (c) 2010 Bertrand Lorentz
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
using System.Collections.Generic;
using NDesk.DBus;

namespace Banshee.Mpris
{
    public delegate void PropertiesChangedHandler (string @interface,
                                                   IDictionary<string, object> changed_properties,
                                                   string [] invalidated_properties);

    [Interface ("org.freedesktop.DBus.Properties")]
    public interface IProperties
    {
        [return: Argument ("value")]
        object Get (string @interface, string propname);
        void Set (string @interface, string propname, object value);
        [return: Argument ("props")]
        IDictionary<string, object> GetAll (string @interface);
        event PropertiesChangedHandler PropertiesChanged;
    }

    [Interface ("org.mpris.MediaPlayer2")]
    public interface IMediaPlayer
    {
        bool CanQuit { get; }
        bool CanRaise { get; }
        bool HasTrackList { get; }
        string Identity { get; }
        string DesktopEntry { get; }
        string [] SupportedUriSchemes { get; }
        string [] SupportedMimeTypes { get; }

        void Quit ();
        void Raise ();
    }
}
