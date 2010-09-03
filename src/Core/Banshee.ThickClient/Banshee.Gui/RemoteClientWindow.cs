// 
// RemoteClientWindow.cs
// 
// Author:
//   Pete Johanson <peter@peterjohanson.com>
// 
// Copyright 2010 Pete Johanson
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Banshee.ServiceStack;

namespace Banshee.Gui
{
    [NDesk.DBus.IgnoreMarshalByRefObjectBaseClass]
    public class RemoteClientWindow : MarshalByRefObject, IClientWindow, IRemoteObjectName
    {
        public RemoteClientWindow (string service_name, IClientWindow window)
        {
            this.service_name = service_name;
            this.window = window;
        }

        private string service_name;
        private IClientWindow window;

        #region IClientWindow Members

        public void Present ()
        {
            window.Present ();
        }

        public void Hide ()
        {
            window.Hide ();
        }

        public void Fullscreen ()
        {
            window.Fullscreen ();
        }

        #endregion

        #region IRemoteExportable Members

        IRemoteExportable IRemoteExportable.Parent {
            get { return null; }
        }

        #endregion

        #region IService Members

        string IService.ServiceName {
            get { return service_name; }
        }

        #endregion

        #region IRemoteObjectName Members

        string IRemoteObjectName.ExportObjectName {
            get { return "ClientWindow"; }
        }

        #endregion
    }
}
