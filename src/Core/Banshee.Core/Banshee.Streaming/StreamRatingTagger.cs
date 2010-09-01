//
// StreamRatingTagger.cs
//
// Author:
//   Nicholas Parker <nickbp@gmail.com>
//
// Copyright (C) 2008-2009 Nicholas Parker
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
using System.Collections;
using System.Globalization;

using Banshee.Collection;

namespace Banshee.Streaming
{
    internal static class ID3v2RatingTagger
    {
        // What we call ourselves in POPM tags.
        private static string POPM_our_creator_name = "Banshee";

        // Ordered list of ID3v2 POPM authors to attempt when importing.
        // Banshee must be listed first, to ensure that we give priority to our own ratings.
        // If new entries are added to this list, also make sure that
        // PopmToBanshee and BansheeToPopm are still accurate.
        private static string[] POPM_known_creator_list = {
            POPM_our_creator_name,// This item must be first
            "quodlibet@lists.sacredchao.net",// Quod Libet (their default)
            "Windows Media Player 9 Series",// WMP/Vista
            "no@email",// MediaMonkey
            "mcored@gmail.com" // iTSfv
        };

        // Converts ID3v2 POPM rating to Banshee rating
        private static int PopmToBanshee (byte popm_rating)
        {
            // The following schemes are used by the other POPM-compatible players:
            // WMP/Vista: "Windows Media Player 9 Series" ratings:
            //   1 = 1, 2 = 64, 3=128, 4=196 (not 192), 5=255
            // MediaMonkey: "no@email" ratings:
            //   0.5=26, 1=51, 1.5=76, 2=102, 2.5=128,
            //   3=153, 3.5=178, 4=204, 4.5=230, 5=255
            // Quod Libet: "quodlibet@lists.sacredchao.net" ratings
            //   (but that email can be changed):
            //   arbitrary scale from 0-255
            // Compatible with all these rating scales (what we'll use):
            //   unrated=0, 1=1-63, 2=64-127, 3=128-191, 4=192-254, 5=255
            if (popm_rating == 0x0)// unrated
                return 0;
            if (popm_rating < 0x40)// 1-63
                return 1;
            if (popm_rating < 0x80)// 64-127
                return 2;
            if (popm_rating < 0xC0)// 128-191
                return 3;
            if (popm_rating < 0xFF)// 192-254
                return 4;
            return 5;// 255
        }

        // Converts Banshee rating to ID3v2 POPM rating
        private static byte BansheeToPopm (int banshee_rating)
        {
            switch (banshee_rating) {
            case 1:
                return 0x1;
            case 2:
                return 0x40;// 64
            case 3:
                return 0x80;// 128
            case 4:
                return 0xC0;// 192
            case 5:
                return 0xFF;// 255
            default:
                return 0x0;// unrated/unknown
            }
        }

        private static TagLib.Id3v2.Tag GetTag (TagLib.File file)
        {
            try {
                return file.GetTag (TagLib.TagTypes.Id3v2) as TagLib.Id3v2.Tag;
            } catch (System.NullReferenceException e) {
                // TagLib# can crash here on unusual files (Ex: FLAC files with ID3v2 metadata)
                // Perhaps FLAC+ID3v2 is an unsupported combination for TagLib#?
                Hyena.Log.WarningFormat ("Got exception when accessing ID3v2 Metadata in {0}:",
                                         file.Name);
                Hyena.Log.Warning (e.Message);
                Hyena.Log.Warning (e.StackTrace);
                return null;
            }
        }

        // Overwrites all POPM frames with the new rating and playcount.
        // If no *known-compatible* frames are found, a new "Banshee"-authored
        // frame is also created to store this information.
        public static void StoreRatingAndPlayCount (int rating, int playcount,
                                                    TagLib.File to_file)
        {
            TagLib.Id3v2.Tag id3v2tag = GetTag (to_file);
            if (id3v2tag == null) {
                return;
            }

            bool known_frames_found = false;
            foreach (TagLib.Id3v2.PopularimeterFrame popm in
                     id3v2tag.GetFrames<TagLib.Id3v2.PopularimeterFrame> ()) {
                if (System.Array.IndexOf (POPM_known_creator_list, popm.User) >= 0) {
                    // Found a known-good POPM frame, don't need to create a "Banshee" frame.
                    known_frames_found = true;
                }

                popm.Rating = BansheeToPopm (rating);
                popm.PlayCount = (ulong)playcount;
                Hyena.Log.DebugFormat ("Exporting ID3v2 Rating={0}({1}) and Playcount={2}({3}) to File \"{4}\" as Creator \"{5}\"",
                                       rating, popm.Rating,
                                       playcount, popm.PlayCount,
                                       to_file.Name, popm.User);
            }

            if (!known_frames_found) {
                // No known-good frames found, create a new POPM frame (with creator string "Banshee")
                TagLib.Id3v2.PopularimeterFrame popm = TagLib.Id3v2.PopularimeterFrame.Get (id3v2tag,
                                                                                            POPM_our_creator_name,
                                                                                            true);
                popm.Rating = BansheeToPopm (rating);
                popm.PlayCount = (ulong)playcount;
                Hyena.Log.DebugFormat ("Exporting ID3v2 Rating={0}({1}) and Playcount={2}({3}) to File \"{4}\" as Creator \"{5}\"",
                                       rating, popm.Rating,
                                       playcount, popm.PlayCount,
                                       to_file.Name, POPM_our_creator_name);
            }
        }

        // Scans the file for *known-compatible* POPM frames, with priority given to
        // frames at the top of the known creator list.
        public static void GetRatingAndPlayCount (TagLib.File from_file,
                                                  ref int rating, ref int playcount)
        {
            TagLib.Id3v2.Tag id3v2tag = GetTag (from_file);
            if (id3v2tag == null) {
                return;
            }

            TagLib.Id3v2.PopularimeterFrame popm = null;
            for (int i = 0; i < POPM_known_creator_list.Length; i++) {
                popm = TagLib.Id3v2.PopularimeterFrame.Get (id3v2tag,
                                                            POPM_known_creator_list[i],
                                                            false);
                if (popm != null) {
                    break;
                }
            }

            if (popm != null) {
                rating = PopmToBanshee (popm.Rating);
                playcount = (int)popm.PlayCount;
            }
        }
    }

    // Applicable for Vorbis, Speex, and many (most?) FLAC files
    // Follows the naming standard established by the Quod Libet team
    // See: http://code.google.com/p/quodlibet/wiki/Specs_VorbisComments
    internal static class OggRatingTagger
    {
        // What we call ourselves in rating/playcount tags.
        private static string ogg_our_creator_name = "BANSHEE";

        // Prefix to rating field names (lowercase)
        private static string rating_prefix = "RATING:";

        // Prefix to playcount field names (lowercase)
        private static string playcount_prefix = "PLAYCOUNT:";

        // Converts Ogg rating to Banshee rating
        private static int OggToBanshee (string ogg_rating_str)
        {
            double ogg_rating;
            if (Double.TryParse (ogg_rating_str, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out ogg_rating)) {
                // Quod Libet Ogg ratings are stored as a value
                // between 0.0 and 1.0 inclusive, where unrated = 0.5.
                if (ogg_rating == 0.5)// unrated
                    return 0;
                if (ogg_rating > 0.8)// (0.8,1.0]
                    return 5;
                if (ogg_rating > 0.6)// (0.6,0.8]
                    return 4;
                if (ogg_rating > 0.4)// (0.4,0.5),(0.5,0.6]
                    return 3;
                if (ogg_rating > 0.2)// (0.2,0.4]
                    return 2;
                else // [0.0,0.2]
                    return 1;
            }

            Hyena.Log.WarningFormat ("Failed to parse ogg rating string: {0}", ogg_rating_str);
            return 0;
        }

        // Converts Banshee rating to Ogg rating
        private static string BansheeToOgg (int banshee_rating)
        {
            // I went with this scaling so that if we switch to fractional stars
            // in the future (such as "0.25 stars"), we'll have room for that.
            switch (banshee_rating) {
            case 1:
                return "0.2";
            case 2:
                return "0.4";
            case 3:
                return "0.6";
            case 4:
                return "0.8";
            case 5:
                return "1.0";
            default:
                return "0.5";// unrated/unknown
            }
        }

        private static TagLib.Ogg.XiphComment GetTag (TagLib.File file)
        {
            try {
                return file.GetTag (TagLib.TagTypes.Xiph) as TagLib.Ogg.XiphComment;
            } catch (System.NullReferenceException e) {
                // Haven't seen crashes when getting Ogg tags, but just in case..
                // (See commentary for ID3v2 version)
                Hyena.Log.WarningFormat ("Got exception when accessing Ogg Metadata in {0}:",
                                         file.Name);
                Hyena.Log.Warning (e.Message);
                Hyena.Log.Warning (e.StackTrace);
                return null;
            }
        }

        // Scans the file for ogg rating/playcount tags as defined by the Quod Libet standard
        // If a Banshee tag is found, it is given priority.
        // If a Banshee tag is not found, the last rating/playcount tags found are used
        public static void GetRatingAndPlayCount (TagLib.File from_file,
                                                  ref int rating, ref int playcount)
        {
            TagLib.Ogg.XiphComment xiphtag = GetTag (from_file);
            if (xiphtag == null) {
                return;
            }

            bool banshee_rating_done = false, banshee_playcount_done = false;
            string rating_raw = "", playcount_raw = "";

            foreach (string fieldname in xiphtag) {

                if (!banshee_rating_done &&
                    fieldname.ToUpper ().StartsWith (rating_prefix)) {

                    rating_raw = xiphtag.GetFirstField (fieldname);
                    string rating_creator = fieldname.Substring (rating_prefix.Length);
                    if (rating_creator.ToUpper () == ogg_our_creator_name) {
                        // We made this rating, consider it authoritative.
                        banshee_rating_done = true;
                        // Don't return -- we might not have seen a playcount yet.
                    }

                } else if (!banshee_playcount_done &&
                           fieldname.ToUpper ().StartsWith (playcount_prefix)) {

                    playcount_raw = xiphtag.GetFirstField (fieldname);
                    string playcount_creator = fieldname.Substring (playcount_prefix.Length);
                    if (playcount_creator.ToUpper () == ogg_our_creator_name) {
                        // We made this playcount, consider it authoritative.
                        banshee_playcount_done = true;
                        // Don't return -- we might not have seen a rating yet.
                    }
                }
            }
            if (rating_raw != "") {
                rating = OggToBanshee (rating_raw);
            }
            if (playcount_raw != "") {
                playcount = int.Parse (playcount_raw);
            }
            Hyena.Log.DebugFormat ("Importing Ogg Rating={0}({1}) and Playcount={2}({3}) from File \"{4}\"",
                                   rating, rating_raw,
                                   playcount, playcount_raw, from_file.Name);
        }

        // Scans the file for ogg rating/playcount tags as defined by the Quod Libet standard
        // All applicable tags are overwritten with the new values, regardless of tag author
        public static void StoreRatingAndPlayCount (int rating, int playcount,
                                                    TagLib.File to_file)
        {
            TagLib.Ogg.XiphComment xiphtag = GetTag (to_file);
            if (xiphtag == null) {
                return;
            }

            ArrayList rating_fieldnames = new ArrayList ();
            ArrayList playcount_fieldnames = new ArrayList ();

            // Collect list of rating and playcount tags to be updated:
            foreach (string fieldname in xiphtag) {
                if (fieldname.ToUpper ().StartsWith (rating_prefix)) {
                    rating_fieldnames.Add (fieldname);
                } else if (fieldname.ToUpper ().StartsWith (playcount_prefix)) {
                    playcount_fieldnames.Add (fieldname);
                }
            }
            // Add "BANSHEE" tags if no rating/playcount tags were found:
            if (rating_fieldnames.Count == 0) {
                rating_fieldnames.Add (rating_prefix+ogg_our_creator_name);
            }
            if (playcount_fieldnames.Count == 0) {
                playcount_fieldnames.Add (playcount_prefix+ogg_our_creator_name);
            }

            string ogg_rating = BansheeToOgg (rating);
            foreach (string ratingname in rating_fieldnames) {
                xiphtag.SetField (ratingname, ogg_rating);
                Hyena.Log.DebugFormat ("Exporting Ogg Rating={0}({1}) to File \"{2}\" as Creator \"{3}\"",
                                       rating, ogg_rating,
                                       to_file.Name,
                                       ratingname.Substring (rating_prefix.Length));
            }
            string ogg_playcount = playcount.ToString ();
            foreach (string playcountname in playcount_fieldnames) {
                xiphtag.SetField (playcountname, ogg_playcount);
                Hyena.Log.DebugFormat ("Exporting Ogg Playcount={0}({1}) to File \"{2}\" as Creator \"{3}\"",
                                       playcount, ogg_playcount,
                                       to_file.Name,
                                       playcountname.Substring (playcount_prefix.Length));
            }
        }
    }

    public static class StreamRatingTagger
    {
        public static void GetRatingAndPlayCount (TagLib.File from_file,
                                                  ref int rating, ref int playcount)
        {
            if ((from_file.Tag.TagTypes & TagLib.TagTypes.Id3v2) != 0) {
                ID3v2RatingTagger.GetRatingAndPlayCount (from_file,
                                                         ref rating, ref playcount);
            }
            if ((from_file.Tag.TagTypes & TagLib.TagTypes.Xiph) != 0) {
                OggRatingTagger.GetRatingAndPlayCount (from_file,
                                                       ref rating, ref playcount);
            }
        }

        public static void StoreRatingAndPlayCount (int rating, int playcount,
                                                    TagLib.File to_file)
        {
            if ((to_file.Tag.TagTypes & TagLib.TagTypes.Id3v2) != 0) {
                ID3v2RatingTagger.StoreRatingAndPlayCount (rating, playcount,
                                                           to_file);
            }
            if ((to_file.Tag.TagTypes & TagLib.TagTypes.Xiph) != 0) {
                OggRatingTagger.StoreRatingAndPlayCount (rating, playcount,
                                                         to_file);
            }
        }
    }
}
