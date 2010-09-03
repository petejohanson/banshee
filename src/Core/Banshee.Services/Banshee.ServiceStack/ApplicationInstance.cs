// 
// ApplicationInstance.cs
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

using Hyena;

namespace Banshee.ServiceStack
{
    public static class ApplicationInstance
    {
        private static IApplicationInstance instance;

        public static bool AlreadyRunning {
            get { return instance.AlreadyRunning; }
        }

        public static bool ConnectTried {
            get { return instance.ConnectTried; }
        }

        public static void Connect ()
        {
            instance.Connect ();
        }

        public static void Create ()
        {
            instance.Create ();
        }

        public static void RunMainLoop ()
        {
            instance.RunMainLoop ();
        }

        public static void QuitMainLoop ()
        {
            instance.QuitMainLoop ();
        }

        static ApplicationInstance ()
        {
            if (Application.IsMSDotNet || ApplicationContext.CommandLine.Contains ("disable-dbus")) {
                instance = new IpcRemotingApplicationInstance ();
            } else {
                instance = new DBusApplicationInstance ();
            }
        }
    }
}
