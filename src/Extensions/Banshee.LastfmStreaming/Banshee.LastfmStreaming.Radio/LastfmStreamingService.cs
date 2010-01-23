
using System;

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
        
        public void Dispose ()
        {
            ServiceManager.Get<DBusCommandService> ().ArgumentPushed -= OnCommandLineArgument;
        }

        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            }
        }

        private bool ServiceStartup ()
        {
            foreach (var src in ServiceManager.SourceManager.FindSources<LastfmSource> ()) {
                lastfm_source = src;
                break;
            }
            
            if (lastfm_source == null) {
                return false;
            }
            
            lastfm_source.ClearChildSources ();
            //lastfm_source.PauseSorting ();
            foreach (StationSource child in StationSource.LoadAll (lastfm_source, lastfm_source.Account.UserName)) {
                lastfm_source.AddChildSource (child);
            }
            //lastfm_source.ResumeSorting ();
            lastfm_source.SortChildSources ();
            return true;
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
        
        string IService.ServiceName {
            get { return "LastfmStreamingService"; }
        }
    }
}
