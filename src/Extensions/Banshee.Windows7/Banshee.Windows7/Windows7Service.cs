//
// Windows7Service.cs
//
// Authors:
//   Pete Johanson <peter@peterjohanson.com>
//
// Copyright (C) 2010 Pete Johanson
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
using System.Drawing;
using System.Linq;
using System.Text;

using Banshee.ServiceStack;
using Banshee.Gui;

using Windows7Support;

namespace Banshee.Windows7
{
    public class Windows7Service : IExtensionService
    {
        GtkElementsService elements_service;
        InterfaceActionService interface_action_service;

        private IList<ThumbnailToolbarButton> buttons;

        bool ServiceStart ()
        {
            if (elements_service == null || interface_action_service == null)
                return false;

            buttons = new List<ThumbnailToolbarButton> () {
                interface_action_service.PlaybackActions["PreviousAction"].CreateThumbnailToolbarButton (a => new Icon (GetResourceStream ("media-skip-backward.ico"))),
                interface_action_service.PlaybackActions["PlayPauseAction"].CreateThumbnailToolbarButton (a => a.StockId == Gtk.Stock.MediaPlay ? new Icon (GetResourceStream ("media-playback-start.ico")) : new Icon (GetResourceStream ("media-playback-pause.ico"))),
                interface_action_service.PlaybackActions["NextAction"].CreateThumbnailToolbarButton (a => new Icon (GetResourceStream ("media-skip-forward.ico"))),
            };

            ServiceManager.ServiceStarted -= OnServiceStarted;

            GtkWindowThumbnailToolbarManager.Register (elements_service.PrimaryWindow, tb => { tb.Buttons = buttons; });

            return true;
        }

        System.IO.Stream GetResourceStream (string resource_name)
        {
            string name = typeof (Windows7Service).Assembly.GetManifestResourceNames ().FirstOrDefault (n => n.EndsWith (resource_name));

            if (String.IsNullOrEmpty (name))
                throw new ArgumentException (String.Format ("Resource named '{0}' not located", resource_name));

            return typeof (Windows7Service).Assembly.GetManifestResourceStream (name);
        }

        void OnServiceStarted (ServiceStartedArgs args)
        {
            if (args.Service is GtkElementsService) {
                elements_service = (GtkElementsService)args.Service;
                ServiceStart ();
            } else if (args.Service is InterfaceActionService) {
                interface_action_service = (InterfaceActionService)args.Service;
                ServiceStart ();
            }
        }        

        #region IExtensionService Members

        public void Initialize ()
        {
            elements_service = ServiceManager.Get<GtkElementsService> ();
            interface_action_service = ServiceManager.Get<InterfaceActionService> ();

            if (!ServiceStart ()) {
                ServiceManager.ServiceStarted += OnServiceStarted;
            }
        }

        #endregion

        #region IService Members

        public string ServiceName {
            get { return "Windows7Service"; }
        }

        #endregion

        #region IDisposable Members

        public void Dispose ()
        {
        }

        #endregion
    }
}
