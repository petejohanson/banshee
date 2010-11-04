//
// Tests.cs
//
// Author:
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

#if ENABLE_TESTS

using System;
using System.Threading;

using NUnit.Framework;

using Hyena;
using Hyena.Tests;

using Banshee.Collection.Database;
using Banshee.Collection;
using Banshee.ServiceStack;

namespace Banshee.MediaEngine
{
    [TestFixture]
    public class Tests : TestBase
    {
        PlayerEngineService service;
        Random rand = new Random ();

        [Test]
        public void TestMediaEngineService ()
        {
            AssertTransition (() => service.Volume = 5, PlayerEvent.Volume);

            for (int i = 0; i < 3; i++) {
                WaitFor (PlayerState.Idle);

                // Assert the default, just-started-up idle state
                Assert.IsFalse (service.IsPlaying ());
                Assert.AreEqual (null, service.CurrentTrack);
                Assert.AreEqual (null, service.CurrentSafeUri);

                LoadAndPlay ("A_boy.ogg");

                for (int j = 0; j < 4; j++) {
                    AssertTransition (() => service.Pause (), PlayerState.Paused);
                    AssertTransition (() => service.Play (), PlayerState.Playing);
                    Assert.IsTrue (service.IsPlaying ());
                    Thread.Sleep ((int) (rand.NextDouble () * 100));
                }

                AssertTransition (() => service.Position = service.Length - 200, PlayerEvent.Seek);
                WaitFor (PlayerState.Idle, PlayerEvent.EndOfStream, PlayerEvent.RequestNextTrack);

                service.Close (true);
            }

            play_when_idles = 0;
            service.Play ();
            Thread.Sleep (50);
            Assert.AreEqual (1, play_when_idles);
            Assert.AreEqual (PlayerState.Idle, service.CurrentState);

            LoadAndPlay ("A_boy.ogg");
            AssertTransition (() => service.TrackInfoUpdated (), PlayerEvent.TrackInfoUpdated);
            LoadAndPlay ("A_girl.ogg");
            AssertTransition (() => service.TrackInfoUpdated (), PlayerEvent.TrackInfoUpdated);

            AssertTransition (() => service.Dispose (), PlayerState.Paused, PlayerState.Idle);
        }

        private void LoadAndPlay (string filename)
        {
            track_intercepts = 0;
            var uri = new SafeUri (Paths.Combine (TestsDir, "data", filename));
            var states = service.IsPlaying () ? new object [] { PlayerState.Paused, PlayerState.Idle, PlayerState.Loading } : new object [] { PlayerState.Loading };
            //var states = service.IsPlaying () ? new object [] { PlayerState.Paused, PlayerState.Loading } : new object [] { PlayerState.Loading };
            if (rand.NextDouble () > .5) {
                AssertTransition (() => service.Open (new UnknownTrackInfo (uri)), states);
            } else {
                AssertTransition (() => service.Open (uri), states);
            }
            Assert.AreEqual (1, track_intercepts);

            // Sleep just a bit to ensure we didn't change from Loading
            Thread.Sleep (30);
            Assert.AreEqual (PlayerState.Loading, service.CurrentState);

            // Assert conditions after Opening (but not actually playing) a track
            Assert.AreEqual (uri, service.CurrentSafeUri);
            Assert.IsTrue (service.CanPause);
            Assert.IsTrue (service.IsPlaying ());
            Assert.IsTrue (service.Position == 0);
            Assert.IsTrue (service.IsPlaying (service.CurrentTrack));

            AssertTransition (() => service.Play (),
                PlayerState.Loaded, PlayerEvent.StartOfStream, PlayerState.Playing);
            Assert.IsTrue (service.Length > 0);
        }

        private void WaitFor (PlayerState state)
        {
            WaitFor (null, state);
        }

        private void WaitFor (System.Action action, PlayerState state)
        {
            if (service.CurrentState != state) {
                AssertTransition (action, state);
            } else if (action != null) {
                Assert.Fail (String.Format ("Already in state {0} before invoking action", state));
            }
        }

        private void WaitFor (params object [] states)
        {
            AssertTransition (null, states);
        }

        private void AssertTransition (System.Action action, params object [] states)
        {
            int result_count = 0;
            var reset_event = new ManualResetEvent (false);
            var handler = new PlayerEventHandler (a => {
                lock (states) {
                    if (result_count < states.Length) {
                        var sca = a as PlayerEventStateChangeArgs;

                        var last_state = sca != null ? sca.Current : service.CurrentState;
                        var last_event = a.Event;
                        var evnt = (states[result_count] as PlayerEvent?) ?? PlayerEvent.StateChange;
                        var state = states[result_count] as PlayerState?;
                        //Console.WriteLine ("want {0}/{1} but got {2}/{3}", evnt, state, last_event, last_state);
                        result_count++;
                        Assert.AreEqual (evnt, last_event);
                        if (state != null) {
                            Assert.AreEqual (state, last_state);
                        }
                    }
                }
                reset_event.Set ();
            });

            service.ConnectEvent (handler);

            if (action != null) action ();

            while (result_count < states.Length) {
                reset_event.Reset ();
                if (!reset_event.WaitOne (1000)) {
                    //Assert.Fail (String.Format ("Waited 1s for {0}; didn't happen", states[result_count]));
                    Assert.Fail (String.Format ("Waited 1s for state/event, didnt' happen"));
                    break;
                }
            }

            service.DisconnectEvent (handler);
        }

        //[Test]
        //public void TestMediaEngines ()
        //{
            //  * TrackInfoUpdated ()
            //  * CurrentTrack
            //  * CurrentState
            //  * LastState
            //  * Volume
            //  * CanSeek
            //  * Position
            //  * Length
        //}


        /*public void AssertEvent (string evnt)
        {
            var evnt_obj = service.
        }*/

            /* TODO: test:
                public event EventHandler PlayWhenIdleRequest;
                public event TrackInterceptHandler TrackIntercept;
                public event Action<PlayerEngine> EngineBeforeInitialize;
                public event Action<PlayerEngine> EngineAfterInitialize;
                public PlayerEngineService ()
        public void Dispose ()
        public void Open (TrackInfo track)
        public void Open (SafeUri uri)
        public void SetNextTrack (TrackInfo track)
        public void SetNextTrack (SafeUri uri)
        public void OpenPlay (TrackInfo track)
        public void IncrementLastPlayed ()
        public void IncrementLastPlayed (double completed)
        public void Close ()
        public void Close (bool fullShutdown)
        public void Play ()
        public void Pause ()
        public void TogglePlaying ()
        public void TrackInfoUpdated ()
        public bool IsPlaying (TrackInfo track)
        public bool IsPlaying ()
        public TrackInfo CurrentTrack {
        public SafeUri CurrentSafeUri {
        public PlayerState CurrentState {
        public PlayerState LastState {
        public ushort Volume {
        public uint Position {
        public byte Rating {
        public bool CanSeek {
        public bool CanPause {
        public bool SupportsEqualizer {
        public uint Length {
        public PlayerEngine ActiveEngine {
        public PlayerEngine DefaultEngine {
        public IEnumerable<PlayerEngine> Engines {
        public void ConnectEvent (PlayerEventHandler handler)
        public void ConnectEvent (PlayerEventHandler handler, PlayerEvent eventMask)
        public void ConnectEvent (PlayerEventHandler handler, bool connectAfter)
        public void ConnectEvent (PlayerEventHandler handler, PlayerEvent eventMask, bool connectAfter)
        public void DisconnectEvent (PlayerEventHandler handler)
        public void ModifyEvent (PlayerEvent eventMask, PlayerEventHandler handler)
        */

        Thread main_thread;
        GLib.MainLoop main_loop;
        bool started;

        [TestFixtureSetUp]
        public void Setup ()
        {
            GLib.GType.Init ();
            if (!GLib.Thread.Supported) {
                GLib.Thread.Init ();
            }

            ApplicationContext.Debugging = false;
            Application.TimeoutHandler = RunTimeout;
            Application.IdleHandler = RunIdle;
            Application.IdleTimeoutRemoveHandler = IdleTimeoutRemove;
            Application.Initialize ();

            Mono.Addins.AddinManager.Initialize (BinDir);

            main_thread = new Thread (RunMainLoop);
            main_thread.Start ();
            while (!started) {}
        }

        [TestFixtureTearDown]
        public void Teardown ()
        {
            GLib.Idle.Add (delegate { main_loop.Quit (); return false; });
            main_thread.Join ();
            main_thread = null;
        }

        int play_when_idles = 0;
        int track_intercepts = 0;

        private void RunMainLoop ()
        {
            ThreadAssist.InitializeMainThread ();
            ThreadAssist.ProxyToMainHandler = Banshee.ServiceStack.Application.Invoke;

            service = new PlayerEngineService ();

            //service.ConnectEvent (a => { Console.WriteLine ("{0}: {1}", a.Event, service.CurrentState); });
            service.PlayWhenIdleRequest += delegate { play_when_idles++; };
            service.TrackIntercept += delegate { track_intercepts++; return false; };

            ((IInitializeService)service).Initialize ();
            ((IDelayedInitializeService)service).DelayedInitialize ();

            main_loop = new GLib.MainLoop ();
            started = true;
            main_loop.Run ();
        }

        protected uint RunTimeout (uint milliseconds, TimeoutHandler handler)
        {
            return GLib.Timeout.Add (milliseconds, delegate { return handler (); });
        }

        protected uint RunIdle (IdleHandler handler)
        {
            return GLib.Idle.Add (delegate { return handler (); });
        }

        protected bool IdleTimeoutRemove (uint id)
        {
            return GLib.Source.Remove (id);
        }
    }
}

#endif
