//
// FolderImportSource.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
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
using Mono.Unix;
using Gtk;

namespace Banshee.Library.Gui
{
    public class FolderImportSource : IImportSource
    {
        public FolderImportSource ()
        {
        }

        public void Import()
        {
            var chooser = Banshee.Gui.Dialogs.FileChooserDialog.CreateForImport (Catalog.GetString ("Import Folders to Library"), false);

            if (chooser.Run () == (int)ResponseType.Ok) {
                Banshee.ServiceStack.ServiceManager.Get<LibraryImportManager> ().Enqueue (chooser.Uris);
            }

            chooser.Destroy ();
        }

        public string Name {
            get { return Banshee.IO.Provider.LocalOnly ? Catalog.GetString ("Local Folders") : Catalog.GetString ("Folders"); }
        }

        public string ImportLabel {
            get { return Catalog.GetString ("C_hoose Folders..."); }
        }

        public string [] IconNames {
            get { return new string [] { "gtk-open" }; }
        }

        public bool CanImport {
            get { return true; }
        }

        public int SortOrder {
            get { return 10; }
        }

        // Reserve strings in preparation for the forthcoming string freeze.
        public void ReservedStrings ()
        {
            Catalog.GetString ("_Folders to import:");
            Catalog.GetString ("Select Folders");
        }
    }
}
