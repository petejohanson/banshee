//
// Actions.cs
//
// Authors:
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
using System.Linq;

using Mono.Unix;
using Gtk;

using Hyena;

using Banshee.ServiceStack;

namespace Banshee.Audiobook
{
    public class Actions : Banshee.Gui.BansheeActionGroup
    {
        private AudiobookLibrarySource library;

        public Actions (AudiobookLibrarySource library) : base ("Audiobook")
        {
            this.library = library;

            Add (
                new ActionEntry ("AudiobookBookPopup", null, null, null, null, (o, a) => ShowContextMenu ("/AudiobookBookPopup")),
                new ActionEntry ("AudiobookOpen", null, Catalog.GetString ("Open Book"), null, null, OnOpen),
                new ActionEntry ("AudiobookEdit", Stock.Edit,
                    Catalog.GetString ("_Edit Track Information"), "E", null, OnEdit)
            );

            /*AddImportant (
                new ActionEntry ("VisitInternetArchive", Stock.JumpTo, Catalog.GetString ("Visit Archive.org"), null, null, (o, a) => {
                    Banshee.Web.Browser.Open ("http://archive.org");
                })
            );*/

            AddUiFromFile ("GlobalUI.xml");

            Register ();

            library.BooksModel.Selection.Changed += HandleSelectionChanged;
        }

        private void HandleSelectionChanged (object sender, EventArgs args)
        {
            ThreadAssist.ProxyToMain (UpdateActions);
        }

        private void UpdateActions ()
        {
            var selection = library.BooksModel.Selection;
            bool has_selection = selection.Count > 0;
            //bool has_single_selection = selection.Count == 1;

            //UpdateAction ("AudiobookMerge", !has_single_selection, true);
            UpdateAction ("AudiobookEdit", true, has_selection);
        }

        private void OnOpen (object o, EventArgs a)
        {
            var index = library.BooksModel.Selection.FocusedIndex;
            if (index > -1) {
                var book = library.BooksModel[index];
                Console.WriteLine ("Asked to open {0}", book);
            }
        }

        private void OnEdit (object o, EventArgs a)
        {
            //var books = library.BooksModel.SelectedItems;
            library.TrackModel.Selection.SelectAll ();
            Actions.TrackActions["TrackEditorAction"].Activate ();
        }
    }
}
