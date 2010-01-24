
using System;
using System.Collections.Generic;

using Mono.Unix;

using Hyena.Data;

using Banshee.Lastfm.Radio;
using Banshee.ServiceStack;
using Banshee.Sources;

namespace Banshee.LastfmStreaming.Radio
{
    public class LastfmStreamingService : IExtensionService, IDisposable
    {
        private LastfmSource lastfm_source = null;

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

            return true;
        }

        public void Dispose ()
        {
            ServiceManager.Get<DBusCommandService> ().ArgumentPushed -= OnCommandLineArgument;
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
