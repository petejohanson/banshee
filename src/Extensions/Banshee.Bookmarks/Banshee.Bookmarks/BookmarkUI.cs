//
// BookmarkUI.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008-2010 Novell, Inc.
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
using System.Data;
using System.Collections.Generic;
using Gtk;
using Mono.Unix;

using Hyena;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.MediaEngine;
using Banshee.Gui;
using Banshee.ServiceStack;

namespace Banshee.Bookmarks
{
    public class BookmarkUI
    {
        private Menu bookmark_menu;
        private Menu remove_menu;

        private ImageMenuItem bookmark_item;
        private ImageMenuItem new_item;
        private ImageMenuItem remove_item;
        private SeparatorMenuItem separator;

        private List<Bookmark> bookmarks = new List<Bookmark> ();
        private Dictionary<Bookmark, MenuItem> select_items = new Dictionary<Bookmark, MenuItem> ();
        private Dictionary<Bookmark, MenuItem> remove_items = new Dictionary<Bookmark, MenuItem> ();
        private Dictionary<MenuItem, Bookmark> bookmark_map = new Dictionary<MenuItem, Bookmark> ();

        private InterfaceActionService action_service;
        private ActionGroup actions;
        private uint ui_manager_id;

        private static BookmarkUI instance = null;
        public static BookmarkUI Instance {
            get {
                if (instance == null)
                    instance = new BookmarkUI ();
                return instance;
            }
        }

        public static bool Instantiated {
            get { return instance != null; }
        }

        private BookmarkUI ()
        {
            action_service = ServiceManager.Get<InterfaceActionService> ("InterfaceActionService");

            actions = new ActionGroup ("Bookmarks");

            actions.Add (new ActionEntry [] {
                new ActionEntry ("BookmarksAction", null,
                                  Catalog.GetString ("_Bookmarks"), null,
                                  null, null),
                new ActionEntry ("BookmarksAddAction", Stock.Add,
                                  Catalog.GetString ("_Add Bookmark"), "<control>D",
                                  Catalog.GetString ("Bookmark the Position in the Current Track"),
                                  HandleNewBookmark)
            });

            action_service.UIManager.InsertActionGroup (actions, 0);
            ui_manager_id = action_service.UIManager.AddUiFromResource ("BookmarksMenu.xml");
            bookmark_item = action_service.UIManager.GetWidget ("/MainMenu/ToolsMenu/Bookmarks") as ImageMenuItem;
            new_item = action_service.UIManager.GetWidget ("/MainMenu/ToolsMenu/Bookmarks/Add") as ImageMenuItem;

            bookmark_menu = bookmark_item.Submenu as Menu;
            bookmark_item.Selected += HandleMenuShown;

            remove_item = new ImageMenuItem (Catalog.GetString ("_Remove Bookmark"));
            remove_item.Sensitive = false;
            remove_item.Image = new Image (Stock.Remove, IconSize.Menu);

            remove_item.Submenu = remove_menu = new Menu ();
            bookmark_menu.Append (remove_item);

            LoadBookmarks ();
        }

        private void HandleMenuShown (object sender, EventArgs args)
        {
            new_item.Sensitive = (ServiceManager.PlayerEngine.CurrentTrack != null);
        }

        private void HandleNewBookmark (object sender, EventArgs args)
        {
            var track = ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo;
            if (track != null) {
                try {
                    AddBookmark (new Bookmark (track, (int)ServiceManager.PlayerEngine.Position));
                } catch (Exception e) {
                    Log.Exception ("Unable to Add New Bookmark", e);
                }
            }
        }

        private void LoadBookmarks ()
        {
            separator = new SeparatorMenuItem ();

            foreach (Bookmark bookmark in Bookmark.LoadAll ()) {
                AddBookmark (bookmark);
            }

            bookmark_item.ShowAll ();
        }

        public void AddBookmark (Bookmark bookmark)
        {
            if (select_items.ContainsKey (bookmark))
                return;

            bookmarks.Add (bookmark);
            if (bookmarks.Count == 1) {
                bookmark_menu.Append (separator);
                remove_item.Sensitive = true;
            }

            // Add menu item to jump to this bookmark
            ImageMenuItem select_item = new ImageMenuItem (bookmark.Name.Replace ("_", "__"));
            select_item.Image = new Image (Stock.JumpTo, IconSize.Menu);
            select_item.Activated += delegate {
                Console.WriteLine ("item delegate, main thread? {0}", ThreadAssist.InMainThread);
                bookmark.JumpTo ();
            };
            bookmark_menu.Append (select_item);
            select_items[bookmark] = select_item;

            // Add menu item to remove this bookmark
            ImageMenuItem rem = new ImageMenuItem (bookmark.Name.Replace ("_", "__"));
            rem.Image = new Image (Stock.Remove, IconSize.Menu);
            rem.Activated += delegate {
                bookmark.Remove ();
            };
            remove_menu.Append (rem);
            remove_items[bookmark] = rem;
            bookmark_map[rem] = bookmark;

            bookmark_menu.ShowAll ();
        }

        public void RemoveBookmark (Bookmark bookmark)
        {
            if (!remove_items.ContainsKey (bookmark))
                return;

            bookmark_menu.Remove (select_items[bookmark]);
            remove_menu.Remove (remove_items[bookmark]);
            bookmarks.Remove (bookmark);
            select_items.Remove (bookmark);
            bookmark_map.Remove (remove_items[bookmark]);
            remove_items.Remove (bookmark);

            if (bookmarks.Count == 0) {
                bookmark_menu.Remove (separator);
                remove_item.Sensitive = false;
           }
        }

        public void Dispose ()
        {
            action_service.UIManager.RemoveUi (ui_manager_id);
            action_service.UIManager.RemoveActionGroup (actions);
            actions = null;

            instance = null;
        }
    }
}
