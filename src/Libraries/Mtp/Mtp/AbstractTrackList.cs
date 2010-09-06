using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Mtp
{
    public abstract class AbstractTrackList
    {
        private bool saved;
        private List<uint> track_ids;
        private MtpDevice device;

        public abstract uint Count { get; protected set; }
        public abstract string Name { get; set; }
        protected abstract IntPtr TracksPtr { get; set; }

        protected abstract int Create ();
        protected abstract int Update ();

        public bool Saved { get { return saved; } }
        protected MtpDevice Device { get { return device; } }

        public IList<uint> TrackIds {
            get { return track_ids; }
        }

        public AbstractTrackList (MtpDevice device, string name)
        {
            this.device = device;
            track_ids = new List<uint> ();
        }

        internal AbstractTrackList (MtpDevice device, IntPtr tracks, uint count)
        {
            this.device = device;
            this.saved = true;
            this.track_ids = new List<uint> ();

            if (tracks != IntPtr.Zero) {
                for (int i = 0; i < (int) count; i++)
                    track_ids.Add ((uint) Marshal.ReadInt32 (tracks, sizeof (int) * i));
            }
        }

        public void AddTrack (Track track)
        {
            AddTrack (track.FileId);
        }

        public void AddTrack (uint track_id)
        {
            track_ids.Add (track_id);
            Count++;
        }

        public void RemoveTrack (Track track)
        {
            RemoveTrack (track.FileId);
        }

        public void RemoveTrack (uint track_id)
        {
            track_ids.Remove (track_id);
            Count--;
        }

        public void ClearTracks ()
        {
            track_ids.Clear ();
            Count = 0;
        }

        public virtual void Save ()
        {
            Count = (uint) track_ids.Count;

            if (TracksPtr != IntPtr.Zero)
                throw new InvalidOperationException ("TracksPtr must be NULL when Save is called");

            try {
                if (Count > 0) {
                    TracksPtr = Marshal.AllocHGlobal (Marshal.SizeOf (typeof (uint)) * (int)Count);
                    for (int i = 0; i < track_ids.Count; i ++)
                        Marshal.WriteInt32 (TracksPtr, i * sizeof (int), (int) track_ids [i]);
                }

                if (saved) {
                    saved = Update () == 0;
                } else {
                    saved = Create () == 0;
                }
            } finally {
                if (TracksPtr != IntPtr.Zero) {
                    Marshal.FreeHGlobal (TracksPtr);
                    TracksPtr = IntPtr.Zero;
                }
            }
        }
    }
}
