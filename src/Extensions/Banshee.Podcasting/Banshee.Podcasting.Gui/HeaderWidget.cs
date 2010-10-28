//
// HeaderWidget.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
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

using Gtk;
using Mono.Unix;
using System;
using System.Collections.Generic;
using System.Linq;

using Hyena;
using Hyena.Data;
using Migo.Syndication;

using Banshee.PlaybackController;
using Banshee.Sources;
using Banshee.Collection.Database;
using Banshee.Widgets;

namespace Banshee.Podcasting.Gui
{
    public class ModelComboBox<T> : DictionaryComboBox<T> where T : class
    {
        private IListModel<T> model;
        private Func<T, string> text_func;

        public ModelComboBox (IListModel<T> model, Func<T, string> text_func)
        {
            this.model = model;
            this.text_func = text_func;

            model.Reloaded += delegate { ThreadAssist.ProxyToMain (Reload); };
            Reload ();

            var last_active = ActiveValue;

            Changed += delegate {
                if (last_active != ActiveValue) {
                    model.Selection.Clear (false);
                    model.Selection.Select (Active);
                    last_active = ActiveValue;
                }
            };
        }

        private void Reload ()
        {
            var active = ActiveValue;
            Clear ();

            bool set_active = false;
            for (int i = 0; i < model.Count; i++) {
                var item = model[i];
                Add (text_func (item), item);

                if (item.Equals (active)) {
                    ActiveValue = item;
                    set_active = true;
                }
            }

            if (!set_active) {
                Active = 0;
            }
        }
    }

    public class HeaderWidget : HBox
    {
        public HeaderWidget (PodcastSource source)
        {
            ThreadAssist.AssertInMainThread ();
            Spacing = 6;

            var podcast_label = new Label (Catalog.GetString ("_Limit to episodes from"));
            var podcast_combo = new ModelComboBox<Feed> (source.FeedModel, feed => feed.Title);
            podcast_label.MnemonicWidget = podcast_combo;

            var new_check = new CheckButton ("new") { Active = true };
            new_check.Toggled += (o, a) => {
                source.NewFilter.Selection.Clear (false);
                // HACK; 1 == new, 0 == both
                source.NewFilter.Selection.Select (new_check.Active ? 1 : 0);
            };

            var downloaded_check = new CheckButton ("downloaded");
            downloaded_check.Toggled += (o, a) => {
                source.DownloadedFilter.Selection.Clear (false);
                // HACK; 1 == downloaded, 0 == both
                source.DownloadedFilter.Selection.Select (downloaded_check.Active ? 1 : 0);
            };

            PackStart (podcast_label, false, false, 0);
            PackStart (podcast_combo, false, false, 0);
            PackStart (new Label ("that are"), false, false, 0);
            PackStart (new_check, false, false, 0);
            PackStart (downloaded_check, false, false, 0);
        }


        /*private void OnModeComboChanged (object o, EventArgs args)
        {
            var random_by = mode_combo.ActiveValue;
            foreach (var widget in sensitive_widgets) {
                widget.Sensitive = random_by.Id != "off";
            }

            var handler = ModeChanged;
            if (handler != null) {
                handler (this, new EventArgs<RandomBy> (random_by));
            }
        }*/
    }
}
