//
// BansheeActionGroup.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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

using Hyena.Gui;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Sources;

namespace Banshee.Gui
{
    public class BansheeActionGroup : HyenaActionGroup
    {
        private InterfaceActionService action_service;
        private Dictionary<string, string> labels = new Dictionary<string, string> ();
        private Dictionary<string, string> icons = new Dictionary<string, string> ();

        public BansheeActionGroup (string name)
            : this (ServiceManager.Get<InterfaceActionService> (), name)
        {
        }

        public BansheeActionGroup (InterfaceActionService action_service, string name) : base (action_service, name)
        {
            this.action_service = action_service;
        }

        public void UpdateActions (bool visible, bool sensitive, Source source, params string [] action_names)
        {
            foreach (string name in action_names) {
                UpdateAction (name, visible, sensitive, source);
            }
        }

        public void UpdateAction (string action_name, bool visible, bool sensitive, Source source)
        {
            Gtk.Action action = this[action_name];
            UpdateAction (action, visible, sensitive);

            if (source != null && action.Visible) {
                // Save the original label
                if (!labels.ContainsKey (action_name))
                    labels.Add (action_name, action.Label);

                // Save the original icon name
                if (!icons.ContainsKey (action_name))
                    icons.Add (action_name, action.IconName);

                // If this source has a label property for this action, override the current label, otherwise reset it
                // to the original label
                string label = source.Properties.Get<string> (String.Format ("{0}Label", action_name)) ?? labels[action_name];
                action.Label = label;

                // If this source has an icon property for this action, override the current icon, othewise reset it
                // to the original icon
                string icon = source.Properties.Get<string> (String.Format ("{0}IconName", action_name)) ?? icons[action_name];
                if (!String.IsNullOrEmpty (icon)) {
                    action.IconName = icon;
                }
            }
        }

        public InterfaceActionService Actions {
            get { return action_service; }
        }

        public Source ActiveSource {
            get { return ServiceManager.SourceManager.ActiveSource; }
        }

        public virtual PrimarySource ActivePrimarySource {
            get { return (ActiveSource as PrimarySource) ?? (ActiveSource.Parent as PrimarySource) ?? ServiceManager.SourceManager.MusicLibrary; }
        }

        public Gtk.Window PrimaryWindow {
            get { return ServiceManager.Get<GtkElementsService> ().PrimaryWindow; }
        }
    }
}
