//
// FixActions.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright 2010 Novell, Inc.
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
using System.Linq;
using System.Collections.Generic;

using Mono.Unix;

using Gtk;

using Hyena;
using Hyena.Data;
using Hyena.Data.Sqlite;

using Banshee.ServiceStack;
using Banshee.Gui;

namespace Banshee.Fixup
{
    public class FixActions : BansheeActionGroup
    {
        public FixActions () : base ("MetadataFixActions")
        {
            Add (new Gtk.ActionEntry (
                "FixMetadataAction", null,
                Catalog.GetString ("Fix Music Metadata..."), null,
                null, OnFixMetadata
            ));

            AddUiFromFile ("GlobalUI.xml");
        }

        private void OnFixMetadata (object o, EventArgs args)
        {
            var music = ServiceManager.SourceManager.MusicLibrary;

            // Only one fix source at a time
            if (music.Children.Any (c => c is FixSource)) {
                ServiceManager.SourceManager.SetActiveSource (music.Children.First (c => c is FixSource));
                return;
            }

            var src = new FixSource ();
            music.AddChildSource (src);
            ServiceManager.SourceManager.SetActiveSource (src);
        }
    }
}
