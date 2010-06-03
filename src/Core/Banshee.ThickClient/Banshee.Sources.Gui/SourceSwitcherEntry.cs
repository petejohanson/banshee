//
// SourceSwitcherEntry.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
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

using System;
using System.Linq;
using System.Collections.Generic;

using Gtk;
using Cairo;
using Mono.Unix;

using Hyena;
using Hyena.Gui;
using Hyena.Widgets;
using Hyena.Gui.Theming;
using Hyena.Gui.Theatrics;

using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Playlist;

using Banshee.Gui;

namespace Banshee.Sources.Gui
{
    public class SourceSwitcherEntry : Hyena.Widgets.EntryPopup
    {
        private SourceView view;
        private uint hide_timeout_id = 0;

        public SourceSwitcherEntry (SourceView view)
        {
            this.view = view;

            HideAfterTimeout = false;

            // FIXME not sure if it's possible to do auto-complete w/o a Model
            /*var completion = new EntryCompletion () {
                //InlineSelection = true,
                //InlineCompletion = true,
                PopupCompletion = true,
                MatchFunc = (c, key, iter) => {
                    //Console.WriteLine ("MatchFunc called! for key {0}", key);
                    return true;
                },
                PopupSingleMatch = true,
                MinimumKeyLength = 1
            };

            var store = new ListStore (typeof (string));
            completion.TextColumn = 0;
            completion.Model = store;
            completion.PopupCompletion = true;
            completion.PopupSingleMatch = true;

            Entry.Completion = completion;
            completion.ActionActivated += (o2, a) => {
                Console.WriteLine ("completion.ActionActivated");
                try {
                    SwitchSource (SourceSwitcherMatches (Text).Skip (a.Index).FirstOrDefault ());
                } catch {}
            };*/

            Changed += delegate {
                //store.Clear ();
                //completion.Complete ();

                if (hide_timeout_id != 0) {
                    ServiceStack.Application.IdleTimeoutRemove (hide_timeout_id);
                    hide_timeout_id = 0;
                }

                if (Text.Length > 0) {
                    // If there is only one source that matches, switch to it
                    var list = SourceSwitcherMatches (Text).ToList ();
                    if (list.Count == 1) {
                        SwitchSource (list[0]);
                        // Hide only after a timeout, helps to capture extra entered chars if we switch before the user expects
                        hide_timeout_id = ServiceStack.Application.RunTimeout (1000, delegate { Hide (); return false; });
                    } else {
                        /*foreach (var src in list) {
                            store.AppendValues (src.Name);
                        }*/
                    }
                }
            };

            Entry.Activated += delegate {
                try {
                    var src = SourceSwitcherMatches (Text).FirstOrDefault ();
                    SwitchSource (src);
                } catch {}
            };

            Position (view.GdkWindow);
            HasFocus = true;
            Show ();
        }

        private IEnumerable<Source> SourceSwitcherMatches (string query)
        {
            query = StringUtil.SearchKey (query);
            if (String.IsNullOrEmpty (query)) {
                return Enumerable.Empty<Source> ();
            }

            //Console.WriteLine ("\nGetting matches for {0}", query);
            return ServiceManager.SourceManager.Sources
                                               .Select  (s => new { Source = s, Priority = SourceSwitcherPriority (s, query) })
                                               .Where   (s => s.Priority > 0)
                                               .OrderBy (s => s.Priority)
                                               .Select  (s => s.Source);
        }

        private int SourceSwitcherPriority (Source s, string query)
        {
            int priority = 0;
            var name = StringUtil.SearchKey (s.Name);
            if (name != null && !String.IsNullOrEmpty (query)) {
                if (name == query) {
                    //Console.WriteLine ("{0} equals {1}", s.Name, query);
                    priority = 10;
                } else if (name.StartsWith (query)) {
                    //Console.WriteLine ("{0} starts with {1}", s.Name, query);
                    priority = 20;
                } else {
                    var split_name = name.Split (new char [] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    if (split_name.Length == query.Length &&
                        Enumerable.Range (0, query.Length).All (i => split_name[i][0] == query[i])) {
                        //Console.WriteLine ("{0} initials are {1}", s.Name, query);
                        priority = 30;
                    } else if (name.Contains (query)) {
                        //Console.WriteLine ("{0} contains {1}", s.Name, query);
                        priority = 40;
                    }
                }

                // Give sources under (or siblings of) the currently active source a priority bump
                var asrc = ServiceManager.SourceManager.ActiveSource;
                if (s.Parent != null && (s.Parent == asrc || s.Parent == asrc.Parent)) {
                    //Console.WriteLine ("{0} is child of active, giving bump", s.Name);
                    priority--;
                }
            }

            return priority;
        }

        private void SwitchSource (Source src)
        {
            if (src != null) {
                if (src.Parent != null) {
                    view.Expand (src.Parent);
                }

                ServiceManager.SourceManager.SetActiveSource (src);

                if (hide_timeout_id != 0) {
                    ServiceStack.Application.IdleTimeoutRemove (hide_timeout_id);
                    hide_timeout_id = 0;
                }
            }
        }
    }
}
