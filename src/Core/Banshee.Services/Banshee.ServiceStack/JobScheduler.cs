//
// JobScheduler.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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

using Hyena.Jobs;

namespace Banshee.ServiceStack
{
    public class JobScheduler : Scheduler, IRequiredService
    {
        class StartupJob : Job
        {
            public StartupJob ()
            {
                Title = "Startup Job";
                PriorityHints = PriorityHints.SpeedSensitive;
                SetResources (Resource.Cpu, Resource.Disk, Resource.Database);
                IsBackground = true;
            }

            public void Finish ()
            {
                OnFinished ();
            }
        }

        StartupJob startup_job = new StartupJob ();

        public JobScheduler ()
        {
            // This startup job blocks any other jobs from starting
            // until we're up and running.
            Add (startup_job);
            Application.ClientStarted += OnClientStarted;
        }

        private void OnClientStarted (Client client)
        {
            Application.ClientStarted -= OnClientStarted;
            Application.RunTimeout (1000, delegate {
                startup_job.Finish ();
                return false;
            });
        }

        string IService.ServiceName {
            get { return "JobScheduler"; }
        }
    }
}
