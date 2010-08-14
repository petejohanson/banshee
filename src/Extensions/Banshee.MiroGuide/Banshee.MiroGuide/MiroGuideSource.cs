//
// MiroGuideSource.cs
//
// Authors:
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

using System;
using System.Linq;
using Mono.Unix;

using Mono.Addins;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Sources.Gui;

using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.MediaEngine;
using Banshee.PlaybackController;

namespace Banshee.MiroGuide
{
    public class MiroGuideSource : Banshee.WebSource.WebSource, IDisposable
    {
        private SourceMessage teaser;

        public MiroGuideSource () : base (Catalog.GetString ("Miro Guide"), 160, "miro-guide")
        {
            Properties.SetString ("Icon.Name", "miro-guide-source");

            if (!MaybeShowTeaserInPodcasts ()) {
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            }
        }

        public void Dispose ()
        {
            if (teaser != null) {
                var podcast_src = ServiceManager.SourceManager.Sources.FirstOrDefault (s => s.UniqueId == "PodcastSource-PodcastLibrary");
                if (podcast_src != null) {
                    podcast_src.RemoveMessage (teaser);
                }
                teaser = null;
            }
        }

        protected override Banshee.WebSource.WebBrowserShell GetWidget ()
        {
            var view = new View ();
            var shell = new Banshee.WebSource.WebBrowserShell (Name, view);
            view.Shell = shell;
            view.UpdateSearchText ();
            return shell;
        }

        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (args.Source.UniqueId == "PodcastSource-PodcastLibrary") {
                MaybeShowTeaserInPodcasts ();
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            }
        }

        private bool MaybeShowTeaserInPodcasts ()
        {
            var manager = ServiceManager.SourceManager;
            var podcast_src = manager.Sources.FirstOrDefault (s => s.UniqueId == "PodcastSource-PodcastLibrary");

            if (podcast_src != null) {
                var show = CreateSchema<bool> ("show_miro_guide_teaser_in_podcasts", true, null, null);
                if (show.Get ()) {
                    var msg = new SourceMessage (podcast_src) {
                        CanClose = true,
                        Text = Catalog.GetString ("Discover interesting podcasts in the Miro Guide podcast directory!")
                    };
                    msg.SetIconName ("miro-guide-source");
                    msg.AddAction (new MessageAction (Catalog.GetString ("Open Miro Guide"),
                        delegate { manager.SetActiveSource (this); }
                    ));
                    msg.Updated += delegate {
                        if (msg.IsHidden) {
                            show.Set (false);
                        }
                    };

                    teaser = msg;
                    podcast_src.PushMessage (msg);
                }
                return true;
            }

            return false;
        }
    }
}
