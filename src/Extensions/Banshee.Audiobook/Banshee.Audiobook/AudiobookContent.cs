//
// AudiobookContent.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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

using Mono.Unix;
using Gtk;

using Hyena;
using Hyena.Data;
using Hyena.Data.Gui;

using Banshee.Widgets;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Collection.Gui;
using Banshee.Gui;
using Banshee.Gui.Widgets;
using Banshee.Sources.Gui;
using Banshee.Web;

namespace Banshee.Audiobook
{
    public class AudiobookContent : ISourceContents
    {
        private AudiobookLibrarySource library;
        private AudiobookGrid grid;
        private ScrolledWindow sw;

        public AudiobookContent ()
        {
            sw = new ScrolledWindow ();
            grid = new AudiobookGrid ();
            sw.Child = grid;
            sw.ShowAll ();
        }

        public bool SetSource (ISource src)
        {
            if (src != null && src == library)
                return true;

            library = src as AudiobookLibrarySource;
            if (library == null) {
                return false;
            }

            grid.SetLibrary (library);

            // Not sure why this is needed
            library.BooksModel.Reloaded += delegate {
                grid.QueueDraw ();
            };
            return true;
        }

        public ISource Source {
            get { return library; }
        }

        public void ResetSource ()
        {
            library = null;
        }

        public Widget Widget {
            get { return sw; }
        }
    }
}
