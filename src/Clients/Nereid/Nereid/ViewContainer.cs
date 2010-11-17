//
// ViewContainer.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using Gtk;
using Mono.Unix;

using Banshee.Widgets;
using Banshee.Gui.Widgets;
using Banshee.Sources.Gui;
using Banshee.Collection;

using Banshee.Gui;
using Banshee.ServiceStack;

namespace Nereid
{
    public class ViewContainer : VBox
    {
        private SearchEntry search_entry;
        private Alignment source_actions_align;
        private EventBox source_actions_box;

        private Banshee.ContextPane.ContextPane context_pane;
        private VBox footer;

        private ISourceContents content;

        public ViewContainer ()
        {
            BuildHeader ();

            Spacing = 6;
            SearchSensitive = false;
        }

        private void BuildHeader ()
        {
            source_actions_align = new Gtk.Alignment (0f, .5f, 1f, 0f) {
                RightPadding = 0,
                LeftPadding = 0,
                NoShowAll = true
            };

            if (Hyena.PlatformDetection.IsMeeGo) {
                source_actions_align.RightPadding = 5;
                source_actions_align.TopPadding = 5;
            }

            footer = new VBox ();

            source_actions_box = new EventBox () { Visible = true };

            BuildSearchEntry ();

            InterfaceActionService uia = ServiceManager.Get<InterfaceActionService> ();
            if (uia != null) {
                Gtk.Action action = uia.GlobalActions["WikiSearchHelpAction"];
                if (action != null) {
                    MenuItem item = new SeparatorMenuItem ();
                    item.Show ();
                    search_entry.Menu.Append (item);

                    item = new ImageMenuItem (Stock.Help, null);
                    item.Activated += delegate { action.Activate (); };
                    item.Show ();
                    search_entry.Menu.Append (item);
                }
            }

            source_actions_box.ShowAll ();
            source_actions_align.Add (source_actions_box);
            source_actions_align.Hide ();
            search_entry.Show ();


            context_pane = new Banshee.ContextPane.ContextPane ();
            context_pane.ExpandHandler = b => {
                SetChildPacking (content.Widget, !b, true, 0, PackType.Start);
                SetChildPacking (context_pane, b, b, 0, PackType.End);
            };

            // Top to bottom, their order is reverse of this:
            PackEnd (footer, false, false, 0);
            PackEnd (context_pane, false, false, 0);
            PackEnd (source_actions_align, false, false, 0);
            PackEnd (new ConnectedMessageBar (), false, true, 0);
        }

        private struct SearchFilter
        {
            public int Id;
            public string Field;
            public string Title;
        }

        private Dictionary<int, SearchFilter> search_filters = new Dictionary<int, SearchFilter> ();

        private void AddSearchFilter (TrackFilterType id, string field, string title)
        {
            SearchFilter filter = new SearchFilter ();
            filter.Id = (int)id;
            filter.Field = field;
            filter.Title = title;
            search_filters.Add (filter.Id, filter);
        }

        private void BuildSearchEntry ()
        {
            AddSearchFilter (TrackFilterType.None, String.Empty, Catalog.GetString ("Artist, Album, or Title"));
            AddSearchFilter (TrackFilterType.SongName, "title", Catalog.GetString ("Track Title"));
            AddSearchFilter (TrackFilterType.ArtistName, "artist", Catalog.GetString ("Artist Name"));
            AddSearchFilter (TrackFilterType.AlbumArtist, "albumartist", Catalog.GetString ("Album Artist"));
            AddSearchFilter (TrackFilterType.AlbumTitle, "album", Catalog.GetString ("Album Title"));
            AddSearchFilter (TrackFilterType.Genre, "genre", Catalog.GetString ("Genre"));
            AddSearchFilter (TrackFilterType.Year, "year", Catalog.GetString ("Year"));
            AddSearchFilter (TrackFilterType.Comment, "comment", Catalog.GetString ("Comment"));

            search_entry = new SearchEntry ();
            search_entry.SetSizeRequest (260, -1);

            foreach (SearchFilter filter in search_filters.Values) {
                search_entry.AddFilterOption (filter.Id, filter.Title);
                if (filter.Id == (int)TrackFilterType.None) {
                    search_entry.AddFilterSeparator ();
                }
            }

            search_entry.FilterChanged += OnSearchEntryFilterChanged;
            search_entry.ActivateFilter ((int)TrackFilterType.None);

            OnSearchEntryFilterChanged (search_entry, EventArgs.Empty);
        }

        private void OnSearchEntryFilterChanged (object o, EventArgs args)
        {
            /*search_entry.EmptyMessage = String.Format (Catalog.GetString ("Filter on {0}"),
                search_entry.GetLabelForFilterID (search_entry.ActiveFilterID));*/

            string query = search_filters.ContainsKey (search_entry.ActiveFilterID)
                ? search_filters[search_entry.ActiveFilterID].Field
                : String.Empty;

            search_entry.Query = String.IsNullOrEmpty (query) ? String.Empty : query + ":";

            Editable editable = search_entry.InnerEntry as Editable;
            if (editable != null) {
                editable.Position = search_entry.Query.Length;
            }
        }

        public void SetHeaderWidget (Widget widget)
        {
            if (widget != null) {
                source_actions_box.Add (widget);
                widget.Show ();
                source_actions_align.Show ();
            }
        }

        public void ClearHeaderWidget ()
        {
            source_actions_align.Hide ();
            if (source_actions_box.Child != null) {
                source_actions_box.Remove (source_actions_box.Child);
            }
        }

        public void SetFooter (Widget contents)
        {
            if (contents != null) {
                footer.PackStart (contents, false, false, 0);
                contents.Show ();
                footer.Show ();
            }
        }

        public void ClearFooter ()
        {
            footer.Hide ();
            foreach (Widget child in footer.Children) {
                footer.Remove (child);
            }
        }

        public Alignment Header {
            get { return source_actions_align; }
        }

        public SearchEntry SearchEntry {
            get { return search_entry; }
        }

        [Obsolete]
        public void SetTitleWidget (Widget widget)
        {
            if (widget != null) {
                Hyena.Log.Warning ("Nereid.SourceContents.TitleWidget is no longer used (from {0})", ServiceManager.SourceManager.ActiveSource.Name);
            }
        }

        public ISourceContents Content {
            get { return content; }
            set {
                if (content == value) {
                    return;
                }

                // Hide the old content widget
                if (content != null && content.Widget != null) {
                    content.Widget.Hide ();
                }

                // Add and show the new one
                if (value != null && value.Widget != null) {
                    PackStart (value.Widget, !context_pane.Large, true, 0);
                    value.Widget.Show ();
                }

                // Remove the old one
                if (content != null && content.Widget != null) {
                    Remove (content.Widget);
                }

                content = value;
            }
        }

        [Obsolete]
        public string Title {
            set {}
        }

        public bool SearchSensitive {
            get { return search_entry.Sensitive; }
            set {
                if (search_entry.Visible != value) {
                    search_entry.Sensitive = value;
                    search_entry.Visible = value;
                }
            }
        }
    }
}
