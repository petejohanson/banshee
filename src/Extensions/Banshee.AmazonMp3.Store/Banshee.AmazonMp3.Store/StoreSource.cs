//
// StoreSource.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using Mono.Unix;

using Hyena;

using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.WebBrowser;

namespace Banshee.AmazonMp3.Store
{
    public class StoreSource : Source
    {
        private StoreSourceContents source_contents;

        public StoreSource () : base (Catalog.GetString ("Amazon MP3 Store"),
            Catalog.GetString ("Amazon MP3 Store"), 150, "amazon-mp3-store")
        {
            TypeUniqueId = "amazon-mp3-store";
            Properties.SetString ("Icon.Name", "amazon-mp3-source");
            Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", false);
        }

        public override void Activate ()
        {
            if (source_contents == null) {
                Properties.Set<ISourceContents> ("Nereid.SourceContents",
                    source_contents = new StoreSourceContents (this));
            }

            base.Activate ();
        }

        public override int Count {
            get { return 0; }
        }

        private class StoreSourceContents : ISourceContents
        {
            private StoreSource source;
            private WebBrowserShell browser_shell = new WebBrowserShell ();

            public StoreSourceContents (StoreSource source)
            {
                this.source = source;
            }

            public bool SetSource (ISource source)
            {
                return true;
            }

            public void ResetSource ()
            {
            }

            public Gtk.Widget Widget {
                get { return browser_shell; }
            }

            public ISource Source {
                get { return source; }
            }
        }
    }
}
