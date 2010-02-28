//
// Configuration.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (c) 2010 Novell, Inc.
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

using Banshee.Configuration;

namespace Banshee.Metrics
{
    public class Configuration
    {
        public static void Start ()
        {
            SchemaEntry.SchemaAdded += OnSchemaAdded;
        }

        public static void Stop ()
        {
            SchemaEntry.SchemaAdded -= OnSchemaAdded;
        }

        private static void OnSchemaAdded (string ns, string key, object value)
        {
            if (Array.BinarySearch<string> (fuzzy_keys, key) >= 0 ||
                Array.BinarySearch<string> (exact_keys, String.Format ("{0}/{1}", ns, key)) >= 0)
            {
                BansheeMetrics.Instance.Add (
                    String.Format ("Configuration/{0}/{1}", ns, key), value
                );
            }
        }

        static Configuration ()
        {
            // Prep for BinarySearch
            Array.Sort<string> (fuzzy_keys);
            Array.Sort<string> (exact_keys);
        }

        // Whitelists
        static string [] fuzzy_keys = new string [] {
            "folder_pattern", "file_pattern", "copy_on_import", "move_on_info_save", "write_metadata", "write_rating",
            "replay_gain_enabled", "io_provider", "show_context_pane", "last_context_page", "child_sort_id",
            "separate_by_type", "expanded"
            //"current_filters", // not useful (yet, at least)
        };

        static string [] exact_keys = new string [] {
            "playback/repeat_mode", "playback/shuffle_mode", "player_window/source_view_width", "player_window/show_cover_art",
            "player_window/width", "player_window/height", "player_window/x_pos", "player_window/y_pos",
            "player_window/maximized", "player_window/source_view_row_height", "player_window/source_view_row_padding", "browser/visible",
            "browser/position", "player_engine/equalizer_enabled", "player_engine/equalizer_preset", "plugins.play_queue/clear_on_quit",
            "plugins.play_queue/populate_mode", "plugins.play_queue/played_songs_number", "plugins.play_queue/upcoming_songs_number",
            "core/make_default", "core/remember_make_default", "core/ever_asked_make_default",
            "plugins.mtp/never_sync_albumart", "plugins.mtp/albumart_max_width", "plugins.notification_area/enabled",
            "plugins.notification_area/show_notifications", "plugins.notification_area/notify_on_close", "plugins.notification_area/quit_on_close",
            "plugins.lastfm/enabled", "plugins.lastfm/subscriber", "plugins.bpm/auto_enabled", "plugins.cover_art/enabled",
            "plugins.audioscrobbler/engine_enabled", "core/disable_internet_access", "plugins.file_system_queue/clear_on_quit",
            "import/audio_cd_error_correction", "import/auto_rip_cds", "import/eject_after_ripped", "plugins.audioscrobbler/api_url"
        };
    }
}
