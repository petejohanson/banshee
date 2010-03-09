//
// PlaybackShuffleActions.cs
//
// Author:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Scott Peterson
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
using System.Collections;
using System.Collections.Generic;
using Mono.Unix;
using Gtk;

using Hyena;
using Hyena.Gui;
using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.PlaybackController;
using Banshee.Collection.Database;

namespace Banshee.Gui
{
    public class PlaybackShuffleActions : BansheeActionGroup, IEnumerable<RadioAction>
    {
        private RadioAction active_action;
        private RadioAction saved_action;
        private PlaybackActions playback_actions;
        private const string shuffle_off_action = "Shuffle_off";

        private Dictionary<int, string> shuffle_modes = new Dictionary<int, string> ();

        public RadioAction Active {
            get { return active_action; }
            set {
                active_action = value;
                ServiceManager.PlaybackController.ShuffleMode = shuffle_modes[active_action.Value];
            }
        }

        public new bool Sensitive {
            get { return base.Sensitive; }
            set {
                base.Sensitive = value;
                OnChanged ();
            }
        }

        public event EventHandler Changed;

        public PlaybackShuffleActions (InterfaceActionService actionService, PlaybackActions playbackActions)
            : base (actionService, "PlaybackShuffle")
        {
            playback_actions = playbackActions;
            Actions.AddActionGroup (this);

            Add (new ActionEntry [] {
                new ActionEntry ("ShuffleMenuAction", null,
                    Catalog.GetString ("Shuffle"), null,
                    Catalog.GetString ("Shuffle"), null)
            });

            ServiceManager.PlaybackController.ShuffleModeChanged += OnShuffleModeChanged;
            ServiceManager.PlaybackController.SourceChanged += OnPlaybackSourceChanged;

            SetShuffler (Banshee.Collection.Database.Shuffler.Playback);
        }

        private Shuffler shuffler;
        public void SetShuffler (Shuffler shuffler)
        {
            if (this.shuffler == shuffler) {
                return;
            }

            if (this.shuffler != null) {
                this.shuffler.RandomModeAdded -= OnShufflerChanged;
                this.shuffler.RandomModeRemoved -= OnShufflerChanged;
            }

            this.shuffler = shuffler;
            this.shuffler.RandomModeAdded += OnShufflerChanged;
            this.shuffler.RandomModeRemoved += OnShufflerChanged;

            UpdateActions ();
        }

        private void OnShufflerChanged (RandomBy random_by)
        {
            UpdateActions ();
        }

        private void UpdateActions ()
        {
            // Clear out the old options
            foreach (string id in shuffle_modes.Values) {
                Remove (String.Format ("Shuffle_{0}", id));
            }
            shuffle_modes.Clear ();

            var radio_group = new RadioActionEntry [shuffler.RandomModes.Count];
            int i = 0;

            // Add all the shuffle options
            foreach (var random_by in shuffler.RandomModes) {
                string action_name = String.Format ("Shuffle_{0}", random_by.Id);
                int id = shuffle_modes.Count;
                shuffle_modes[id] = random_by.Id;
                radio_group[i++] = new RadioActionEntry (
                        action_name, null,
                        random_by.Label, null,
                        random_by.Description,
                        id);
            }

            Add (radio_group, 0, OnActionChanged);

            // Set the icons
            foreach (var random_by in shuffler.RandomModes) {
                this[String.Format ("Shuffle_{0}", random_by.Id)].IconName = random_by.IconName ?? "media-playlist-shuffle";
            }
            this[shuffle_off_action].StockId = Gtk.Stock.MediaNext;

            var action = this[ConfigIdToActionName (ShuffleMode.Get ())];
            if (action is RadioAction) {
                Active = (RadioAction)action;
            } else {
                Active = (RadioAction)this[shuffle_off_action];
            }

            Active.Activate ();
            OnChanged ();
        }

        private void OnShuffleModeChanged (object o, EventArgs<string> args)
        {
            if (shuffle_modes[active_action.Value] != args.Value) {
                // This happens only when changing the mode using DBus.
                // In this case we need to locate the action by its value.
                foreach (RadioAction action in this) {
                    if (shuffle_modes[action.Value] == args.Value) {
                        active_action = action;
                        break;
                    }
                }
            }

            if (saved_action == null) {
                ShuffleMode.Set (ActionNameToConfigId (active_action.Name));
            }
            OnChanged();
        }

        private void OnPlaybackSourceChanged (object o, EventArgs args)
        {
            var source = ServiceManager.PlaybackController.Source;

            if (saved_action == null && !source.CanShuffle) {
                saved_action = Active;
                Active = this[shuffle_off_action] as RadioAction;
                Sensitive = false;
            } else if (saved_action != null && source.CanShuffle) {
                Active = saved_action;
                saved_action = null;
                Sensitive = true;
            }
        }

        private void OnActionChanged (object o, ChangedArgs args)
        {
            Active = args.Current;
        }

        private void OnChanged ()
        {
            playback_actions["NextAction"].StockId = Active.StockId;
            playback_actions["NextAction"].IconName = Active.IconName;
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public void AttachSubmenu (string menuItemPath)
        {
            MenuItem parent = Actions.UIManager.GetWidget (menuItemPath) as MenuItem;
            parent.Submenu = CreateMenu ();
        }

        public MenuItem CreateSubmenu ()
        {
            MenuItem parent = (MenuItem)this["ShuffleMenuAction"].CreateMenuItem ();
            parent.Submenu = CreateMenu ();
            return parent;
        }

        public Menu CreateMenu ()
        {
            Menu menu = new Gtk.Menu ();
            bool separator = false;
            foreach (RadioAction action in this) {
                menu.Append (action.CreateMenuItem ());
                if (!separator) {
                    separator = true;
                    menu.Append (new SeparatorMenuItem ());
                }
            }
            menu.ShowAll ();
            return menu;
        }

        public IEnumerator<RadioAction> GetEnumerator ()
        {
            foreach (string id in shuffle_modes.Values) {
                yield return (RadioAction)this[String.Format ("Shuffle_{0}", id)];
            }
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        private static string ConfigIdToActionName (string configuration)
        {
            return "S" + configuration.Substring (1);
        }

        private static string ActionNameToConfigId (string actionName)
        {
            return actionName.ToLowerInvariant ();
        }

        public static readonly SchemaEntry<string> ShuffleMode = new SchemaEntry<string> (
            "playback", "shuffle_mode",
            "off",
            "Shuffle playback",
            "Shuffle mode (shuffle_off, shuffle_song, shuffle_artist, shuffle_album, shuffle_rating, shuffle_score)"
        );
    }
}