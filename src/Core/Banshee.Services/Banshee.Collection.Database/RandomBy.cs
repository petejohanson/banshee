//
// RandomBy.cs
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
using System.Linq;
using System.Collections.Generic;

using Hyena;
using Hyena.Data;
using Hyena.Data.Sqlite;

using Banshee.ServiceStack;
using Banshee.PlaybackController;

namespace Banshee.Collection.Database
{
    /// <summary>
    /// RandomBy Basic Implementation
    /// Implement at least GetPlaybackTrack or GetShufflerTrack
    /// Use Label, Adverb, and Descrption for GUI Labeling
    ///
    /// Protected strings Select,From,Condition and OrderBy are used for ShufflerQuery (example: GetTrack(ShufflerQuery, args[]))
    /// </summary>
    public abstract class RandomBy
    {
        protected DatabaseTrackListModel Model { get; private set; }
        protected IDatabaseTrackModelCache Cache { get; private set; }

        private HyenaSqliteCommand shuffler_query;
        private string filtered_shuffler_condition;

        protected Shuffler Shuffler { get; private set; }

        public string Id { get; private set; }
        public string Label { get; protected set; }
        public string Adverb { get; protected set; }
        public string Description { get; protected set; }
        public string IconName { get; protected set; }

        public virtual bool IsReady { get { return true; } }

        protected string Select { get; set; }
        protected string From { get; set; }
        protected string Condition { get; set; }
        protected string OrderBy { get; set; }

        public RandomBy (string id)
        {
            Id = id;
        }

        public void SetShuffler (Shuffler shuffler)
        {
            if (Shuffler != null)
                throw new InvalidOperationException ("RandomBy already has Shuffler");

            Shuffler = shuffler;
        }

        protected HyenaSqliteCommand ShufflerQuery {
            get {
                if (shuffler_query == null) {
                    var provider = DatabaseTrackInfo.Provider;
                    // TODO also filter on LastPlayed/SkippedStamp if not PlaybackShuffler (eg for manually added songs)
                    shuffler_query = new HyenaSqliteCommand (String.Format (@"
                        SELECT {0} {1}
                            FROM {2} {3} LEFT OUTER JOIN CoreShuffles ON (CoreShuffles.ShufflerId = {4} AND CoreShuffles.TrackID = CoreTracks.TrackID)
                            WHERE {5} {6} AND {7} AND
                                LastStreamError = 0 AND (CoreShuffles.LastShuffledAt < ? OR CoreShuffles.LastShuffledAt IS NULL)
                            ORDER BY {8} LIMIT 1",
                        provider.Select, Select,
                        Model.FromFragment, From, Shuffler.DbId,
                        String.IsNullOrEmpty (provider.Where) ? "1=1" : provider.Where, Model.ConditionFragment ?? "1=1", Condition,
                        OrderBy
                    ));
                }

                return shuffler_query;
            }
        }

        private string CacheCondition {
            get {
                if (filtered_shuffler_condition == null) {
                    filtered_shuffler_condition = String.Format (@"
                        AND {0}
                        AND LastStreamError = 0
                        AND (LastPlayedStamp < ? OR LastPlayedStamp IS NULL)
                        AND (LastSkippedStamp < ? OR LastSkippedStamp IS NULL)
                        ORDER BY {1}",
                        Condition ?? "1=1", OrderBy
                    );
                }
                return filtered_shuffler_condition;
            }
        }

        public void SetModelAndCache (DatabaseTrackListModel model, IDatabaseTrackModelCache cache)
        {
            if (Model != model) {
                Model = model;
                Cache = cache;
                Reset ();

                OnModelAndCacheUpdated ();
            }

            shuffler_query = null;
            filtered_shuffler_condition = null;
        }

        protected virtual void OnModelAndCacheUpdated ()
        {
        }

        public virtual void Reset () {}

        /// <summary>
        /// Returns true if RandomBy Implementation has a next track, depending on parameter after
        /// If Next returns false in the first place, it is called again with another DateTime
        /// </summary>
        /// <param name="after">
        /// A <see cref="DateTime"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public virtual bool Next (DateTime after)
        {
            return true;
        }

        public TrackInfo GetTrack (DateTime after)
        {
            if (!IsReady) {
                return null;
            }

            TrackInfo track = null;
            using (var context = GetQueryContext (after)) {
                var args = context.Parameters.ToList ();
                args.Add (after);

                if (Shuffler == Shuffler.Playback) {
                    // Add a second after arg b/c we query against lastplay/lastskip stamps
                    args.Add (after);
                    track = Cache.GetSingle (Select, From, CacheCondition, args.ToArray ());
                } else {
                    track = GetTrack (ShufflerQuery, args.ToArray ());
                }
            }

            Shuffler.RecordShuffle (track as DatabaseTrackInfo);
            return track;
        }

        public class QueryContext : IDisposable
        {
            public QueryContext () {}
            public virtual IEnumerable<object> Parameters { get; set; }
            public virtual void Dispose () {}
        }

        protected virtual QueryContext GetQueryContext (DateTime after)
        {
            return new QueryContext () {
                Parameters = GetConditionParameters (after)
            };
        }

        public virtual void SetLastTrack (TrackInfo track)
        {
        }

        protected virtual IEnumerable<object> GetConditionParameters (DateTime after)
        {
            yield break;
        }

        protected DatabaseTrackInfo GetTrack (HyenaSqliteCommand cmd, params object [] args)
        {
            using (var reader = ServiceManager.DbConnection.Query (cmd, args)) {
                if (reader.Read ()) {
                    return DatabaseTrackInfo.Provider.Load (reader);
                }
            }

            return null;
        }
    }
}
