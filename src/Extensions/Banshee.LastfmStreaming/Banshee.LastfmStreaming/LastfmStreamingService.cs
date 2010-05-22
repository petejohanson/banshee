//
// LastfmStreamingService.cs
//
// Authors:
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
//
// Copyright 2010 Bertrand Lorentz
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

using Mono.Unix;

using Hyena.Data;

using Banshee.Lastfm;
using Banshee.ServiceStack;
using Banshee.Sources;

namespace Banshee.LastfmStreaming.Radio
{
    public class LastfmStreamingService : IExtensionService, IDisposable
    {
        private LastfmSource lastfm_source = null;
        private LastfmStreamingActions actions;

        public LastfmStreamingService ()
        {
        }
        
        void IExtensionService.Initialize ()
        {
            ServiceManager.Get<DBusCommandService> ().ArgumentPushed += OnCommandLineArgument;
            
            if (!ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            }
        }
        
        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            }
        }

        private bool ServiceStartup ()
        {
            if (lastfm_source != null) {
                return true;
            }

            foreach (var src in ServiceManager.SourceManager.FindSources<LastfmSource> ()) {
                lastfm_source = src;
                break;
            }
            
            if (lastfm_source == null) {
                return false;
            }

            lastfm_source.ClearChildSources ();
            lastfm_source.SetChildSortTypes (station_sort_types);
            //lastfm_source.PauseSorting ();
            foreach (StationSource child in StationSource.LoadAll (lastfm_source, lastfm_source.Account.UserName)) {
                lastfm_source.AddChildSource (child);
            }
            //lastfm_source.ResumeSorting ();
            lastfm_source.SortChildSources ();
            lastfm_source.Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            lastfm_source.Properties.Set<bool> ("ActiveSourceUIResourcePropagate", true);
            lastfm_source.Properties.Set<System.Reflection.Assembly> ("ActiveSourceUIResource.Assembly", typeof(StationSource).Assembly);
            lastfm_source.Properties.SetString ("SortChildrenActionLabel", Catalog.GetString ("Sort Stations by"));

            actions = new LastfmStreamingActions (lastfm_source);
            
            return true;
        }

        public void Dispose ()
        {
            ServiceManager.Get<DBusCommandService> ().ArgumentPushed -= OnCommandLineArgument;
            actions.Dispose ();
            lastfm_source.ClearChildSources ();

            actions = null;
        }

        private void OnCommandLineArgument (string uri, object value, bool isFile)
        {
            if (!isFile || String.IsNullOrEmpty (uri)) {
                return;
            }

            // Handle lastfm:// URIs
            if (uri.StartsWith ("lastfm://")) {
                StationSource.CreateFromUrl (lastfm_source, uri);
            }
        }
        
        // Order by the playCount of a station, then by inverted name
        public class PlayCountComparer : IComparer<Source>
        {
            public int Compare (Source sa, Source sb)
            {
                StationSource a = sa as StationSource;
                StationSource b = sb as StationSource;
                return a.PlayCount.CompareTo (b.PlayCount);
            }
        }

        private static SourceSortType[] station_sort_types = new SourceSortType[] {
            Source.SortNameAscending,
            new SourceSortType (
                "LastfmTotalPlayCount",
                Catalog.GetString ("Total Play Count"),
                SortType.Descending, new PlayCountComparer ())
        };


        string IService.ServiceName {
            get { return "LastfmStreamingService"; }
        }
    }
}
