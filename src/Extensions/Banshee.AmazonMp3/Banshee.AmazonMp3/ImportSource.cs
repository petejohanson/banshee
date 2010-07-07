// 
// ImportSource.cs
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

using Banshee.ServiceStack;
using Banshee.Library;
using Banshee.I18n;

namespace Banshee.AmazonMp3
{
    public sealed class ImportSource : IImportSource
    {
        public void Import ()
        {
            var chooser = Banshee.Gui.Dialogs.FileChooserDialog.CreateForImport (
                Catalog.GetString ("Download Amazon MP3 Purchase"), true);
            var filter = new Gtk.FileFilter () {
                Name = Catalog.GetString ("Amazon MP3 Files")
            };
            filter.AddPattern ("*.amz");
            chooser.AddFilter (filter);

            try {
                if (chooser.Run () == (int)Gtk.ResponseType.Ok) {
                    foreach (var path in chooser.Filenames) {
                        ServiceManager.Get<AmazonMp3DownloaderService> ().DownloadAmz (path);
                    }
                }
            } finally {
                chooser.Destroy ();
            }
        }

        public bool CanImport {
            get { return true; }
        }

        public string Name {
            get { return Catalog.GetString ("Amazon MP3 Purchase"); }
        }

        public string ImportLabel {
            get { return Catalog.GetString ("C_hoose Files"); }
        }

        public string [] IconNames {
            get { return new string [] { "amazon-mp3-source" }; }
        }

        public int SortOrder {
            get { return 40; }
        }
    }
}

