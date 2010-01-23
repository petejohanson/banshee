
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
            //lastfm_source.PauseSorting ();
            foreach (StationSource child in StationSource.LoadAll (lastfm_source, lastfm_source.Account.UserName)) {
                lastfm_source.AddChildSource (child);
            }
            //lastfm_source.ResumeSorting ();
            lastfm_source.SortChildSources ();
            lastfm_source.Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            lastfm_source.Properties.Set<bool> ("ActiveSourceUIResourcePropagate", true);
            
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
        
        string IService.ServiceName {
            get { return "LastfmStreamingService"; }
        }
    }
}
