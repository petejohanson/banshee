//
// PlaybackSubtitleActions.cs
//
// Author:
//   Olivier Dufour <olivier.duff@gmail.com>
//
// Copyright 2010 Olivier Dufour
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
using System.Collections;

using Gtk;

using Banshee.ServiceStack;
using Banshee.I18n;
using Banshee.Collection;

using Hyena;

namespace Banshee.Gui
{
    public class PlaybackSubtitleActions : BansheeActionGroup, IEnumerable<RadioAction>
    {
        private readonly List<RadioActionEntry> embedded_subtitles_actions = new List<RadioActionEntry> ();
        public event EventHandler Changed;
        private Menu mainMenu;

        public new bool Sensitive {
            get { return base.Sensitive; }
            set {
                base.Sensitive = value;
                OnChanged ();
            }
        }

        public PlaybackSubtitleActions (InterfaceActionService actionService)
            : base (actionService, "PlaybackSubtitle")
        {
            Actions.AddActionGroup (this);

            Add (new ActionEntry [] {
                new ActionEntry ("SubtitleMenuAction", null,
                    Catalog.GetString ("Subtitle"), null,
                    Catalog.GetString ("Subtitle"), null),
                new ActionEntry ("LoadSubtitleAction", null,
                    Catalog.GetString ("Load File..."), null,
                    Catalog.GetString ("Load subtitle file"), OnLoadSubtitleAction)
            });

            this["SubtitleMenuAction"].Sensitive = true;

            ServiceManager.PlaybackController.TrackStarted += OnPlaybackTrackStarted;

            //TODO: Set default sub
        }

        private void OnLoadSubtitleAction (object o, EventArgs args)
        {
            var chooser = new Banshee.Gui.Dialogs.FileChooserDialog (
                Catalog.GetString ("Load Subtitle File"),
                ServiceManager.Get<Banshee.Gui.GtkElementsService> ().PrimaryWindow,
                FileChooserAction.Open
            );

            chooser.DefaultResponse = ResponseType.Ok;
            chooser.SelectMultiple = false;

            chooser.AddButton (Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton (Catalog.GetString ("L_oad"), ResponseType.Ok);

            Hyena.Gui.GtkUtilities.SetChooserShortcuts (chooser,
                ServiceManager.SourceManager.VideoLibrary.BaseDirectory
            );

            var filter = new FileFilter();
            filter.AddMimeType ("text/x-pango-markup");
            filter.AddMimeType ("text/plain");
            filter.Name = Catalog.GetString ("Subtitle files");
            chooser.AddFilter (filter);

            if (chooser.Run () == (int)ResponseType.Ok) {
                ServiceManager.PlayerEngine.SubtitleUri = new SafeUri (chooser.Uri);
            }

            chooser.Destroy ();
        }

        private void OnPlaybackTrackStarted (object o, EventArgs args)
        {
            var current_track = ServiceManager.PlaybackController.CurrentTrack;

            if (current_track != null &&
                (current_track.MediaAttributes & TrackMediaAttributes.VideoStream) != 0) {
                //TODO: activate load subtitle file menu else unactivate
            }
        }

        private void ClearEmbeddedSubtitles ()
        {
            foreach (RadioActionEntry action in embedded_subtitles_actions) {
                this.Remove (action.name);
            }
        }

        private void AddEmbeddedSubtitle (int i)
        {
            string desc = ServiceManager.PlayerEngine.GetSubtitleDescription (i);
            if (String.IsNullOrEmpty (desc)) {
                desc = String.Format (Catalog.GetString ("Subtitle {0}"), i);
            }
            RadioActionEntry new_action = new RadioActionEntry (String.Format ("Subtitle{0}", i), null,
                                                                desc, null,
                                                                String.Format (Catalog.GetString ("Activate embedded subtitle {0}"), i), i);
            embedded_subtitles_actions.Add (new_action);

        }

        public void ReloadEmbeddedSubtitle ()
        {
            ClearEmbeddedSubtitles ();
            int sub_count = ServiceManager.PlayerEngine.SubtitleCount;
            if (sub_count == 0) {
                RefreshMenu ();
                return;
            }
            embedded_subtitles_actions.Add (new RadioActionEntry ("None", null,
                                                                  Catalog.GetString ("None"), null,
                                                                  Catalog.GetString ("Hide subtitles"), -1));
            for (int i = 0; i < sub_count; i++) {
                AddEmbeddedSubtitle (i);
            }
            Add (embedded_subtitles_actions.ToArray (), 0, OnActionChanged);
            RefreshMenu ();
        }

        private void OnActionChanged (object o, ChangedArgs args)
        {
            Log.Debug (string.Format ("[sub] Set sub {0}", args.Current.Value));
            ServiceManager.PlayerEngine.SubtitleIndex = args.Current.Value;
        }

        private void OnChanged ()
        {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public IEnumerator<RadioAction> GetEnumerator ()
        {
            foreach (RadioActionEntry entry in embedded_subtitles_actions) {
                yield return (RadioAction)this[entry.name];
            }
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        public void AttachSubmenu (string menuItemPath)
        {
            MenuItem menu = Actions.UIManager.GetWidget (menuItemPath) as MenuItem;
            menu.Submenu = CreateMenu ();
        }

        private void RefreshMenu ()
        {
            foreach (Widget w in mainMenu.Children) {
                //RadioMenuItems are embedded subtitle ones
                if (w is RadioMenuItem) {
                    mainMenu.Remove (w);
                }
            }
            AddEmbeddedSubtitleMenu ();
            mainMenu.ShowAll ();
        }

        public Menu CreateMenu ()
        {
            mainMenu = new Gtk.Menu ();

            mainMenu.Append (this["LoadSubtitleAction"].CreateMenuItem ());
            mainMenu.Append (new SeparatorMenuItem ());
            AddEmbeddedSubtitleMenu ();

            mainMenu.ShowAll ();
            return mainMenu;
        }

        public void AddEmbeddedSubtitleMenu ()
        {
            foreach (RadioAction action in this) {
                mainMenu.Append (action.CreateMenuItem ());
                Log.Debug (string.Format ("[sub] Add {0}", action.Name));
            }
        }
    }
}

