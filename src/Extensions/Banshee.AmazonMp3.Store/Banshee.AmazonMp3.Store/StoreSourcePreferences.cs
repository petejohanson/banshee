// 
// StoreSourcePreferences.cs
// 
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2010 Novell, Inc.
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

using Banshee.ServiceStack;
using Banshee.Preferences;

namespace Banshee.AmazonMp3.Store
{
    public class StoreSourcePreferences : IDisposable
    {
       // private StoreSource source;
        private SourcePage source_page;

        public StoreSourcePreferences (StoreSource source)
        {
            var service = ServiceManager.Get<PreferenceService> ();
            if (service == null) {
                return;
            }

          //  this.source = source;

            service.InstallWidgetAdapters += OnPreferencesServiceInstallWidgetAdapters;
            source_page = new SourcePage (source);
        }

        public void Dispose ()
        {
            var service = ServiceManager.Get<PreferenceService> ();
            if (service == null || source_page == null) {
                return;
            }

            service.InstallWidgetAdapters -= OnPreferencesServiceInstallWidgetAdapters;
            source_page.Dispose ();
            source_page = null;
        }

        private void OnPreferencesServiceInstallWidgetAdapters (object sender, EventArgs args)
        {
        }
    }
}

