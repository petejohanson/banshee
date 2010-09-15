//
// banshee-player-replaygain.c
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Julien Moutte <julien@fluendo.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2010 Fluendo S.A.
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

#include <math.h>
#include "banshee-player-replaygain.h"
#include "banshee-player-pipeline.h"

// ---------------------------------------------------------------------------
// Private Functions
// ---------------------------------------------------------------------------

static gdouble
bp_replaygain_db_to_linear (gdouble value)
{
    return pow (10, value / 20.0);
}

static gdouble bp_rg_calc_history_avg (BansheePlayer *player)
{
    gdouble sum = 0.0;
    int i;
    for (i = 0; i < player->history_size; ++i) {
        sum += player->rg_gain_history[i];
    }
    return sum / player->history_size;
}

static void bp_replaygain_update_history (BansheePlayer *player)
{
    gdouble gain;
    g_return_if_fail (player->history_size <= 10);

    if (player->history_size == 10) {
        memmove (player->rg_gain_history + 1, player->rg_gain_history, sizeof (gdouble) * 9);
    } else {
        memmove (player->rg_gain_history + 1, player->rg_gain_history, sizeof (gdouble) * player->history_size);
        player->history_size++;
    }

    g_object_get (G_OBJECT (player->rgvolume), "target-gain", &gain, NULL);
    player->rg_gain_history[0] = gain;
    bp_debug2 ("[ReplayGain] Added gain: %.2f to history.", gain);

    g_object_set (G_OBJECT (player->rgvolume), "fallback-gain", bp_rg_calc_history_avg (player), NULL);
}

static void on_target_gain_changed (GstElement *rgvolume, GParamSpec *pspec, BansheePlayer *player)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));

    bp_replaygain_update_history (player);
    _bp_rgvolume_print_volume (player);
}

static void
pad_block_cb (GstPad *srcPad, gboolean blocked, gpointer user_data)
{
    BansheePlayer* player;

    if (!blocked) {
        return;
    }

    player = (BansheePlayer*) user_data;
    g_return_if_fail (IS_BANSHEE_PLAYER (player));

    // The pad_block_cb can get triggered multiple times, on different threads.
    // Lock around the link/unlink code, so we don't end up going through here
    // with inconsistent state.
    g_mutex_lock (player->replaygain_mutex);

    if ((player->replaygain_enabled && player->rgvolume_in_pipeline) ||
        (!player->replaygain_enabled && !player->rgvolume_in_pipeline)) {
        // The pipeline is already in the correct state.  Unblock the pad, and return.
        g_mutex_unlock (player->replaygain_mutex);
        if (gst_pad_is_blocked (srcPad)) {
            gst_pad_set_blocked_async (srcPad, FALSE, &pad_block_cb, player);
        }
        return;
    }

    if (player->rgvolume_in_pipeline) {
        gst_element_unlink (player->before_rgvolume, player->rgvolume);
        gst_element_unlink (player->rgvolume, player->after_rgvolume);
    } else {
        gst_element_unlink (player->before_rgvolume, player->after_rgvolume);
    }

    if (player->replaygain_enabled) {
        player->rgvolume = _bp_rgvolume_new (player);
        if (!GST_IS_ELEMENT (player->rgvolume)) {
            player->replaygain_enabled = FALSE;
        }
    } else {
        gst_element_set_state (player->rgvolume, GST_STATE_NULL);
        gst_bin_remove (GST_BIN (player->audiobin), player->rgvolume);
    }

    if (player->replaygain_enabled && GST_IS_ELEMENT (player->rgvolume)) {
        g_signal_connect (player->rgvolume, "notify::target-gain", G_CALLBACK (on_target_gain_changed), player);
        gst_bin_add (GST_BIN (player->audiobin), player->rgvolume);
        gst_element_sync_state_with_parent (player->rgvolume);

        // link in rgvolume and connect to the real audio sink
        gst_element_link (player->before_rgvolume, player->rgvolume);
        gst_element_link (player->rgvolume, player->after_rgvolume);
        player->rgvolume_in_pipeline = TRUE;
    } else {
        // link the queue with the real audio sink
        gst_element_link (player->before_rgvolume, player->after_rgvolume);
        player->rgvolume_in_pipeline = FALSE;
    }

    // Our state is now consistent
    g_mutex_unlock (player->replaygain_mutex);

    if (gst_pad_is_blocked (srcPad)) {
        gst_pad_set_blocked_async (srcPad, FALSE, &pad_block_cb, player);
    }

    _bp_rgvolume_print_volume (player);
}

// ---------------------------------------------------------------------------
// Internal Functions
// ---------------------------------------------------------------------------


GstElement* _bp_rgvolume_new (BansheePlayer *player)
{
    GstElement *rgvolume = gst_element_factory_make ("rgvolume", NULL);

    if (rgvolume == NULL) {
        bp_debug ("Loading ReplayGain plugin failed.");
    }

    return rgvolume;
}

void _bp_rgvolume_print_volume(BansheePlayer *player)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    if (player->replaygain_enabled && (player->rgvolume != NULL)) {
        gdouble scale;

        g_object_get (G_OBJECT (player->rgvolume), "result-gain", &scale, NULL);

        bp_debug4 ("scaled volume: %.2f (ReplayGain) * %.2f (User) = %.2f",
                  bp_replaygain_db_to_linear (scale), player->current_volume,
                  bp_replaygain_db_to_linear (scale) * player->current_volume);
    }
}

void _bp_replaygain_pipeline_rebuild (BansheePlayer* player)
{
    GstPad* srcPad;

    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    g_return_if_fail (GST_IS_ELEMENT (player->before_rgvolume));
    srcPad = gst_element_get_static_pad (player->before_rgvolume, "src");

    if (gst_pad_is_active (srcPad) && !gst_pad_is_blocked (srcPad)) {
        gst_pad_set_blocked_async (srcPad, TRUE, &pad_block_cb, player);
    } else if (srcPad->block_callback == NULL) {
        pad_block_cb (srcPad, TRUE, player);
    }
}

// ---------------------------------------------------------------------------
// Public Functions
// ---------------------------------------------------------------------------

P_INVOKE void
bp_replaygain_set_enabled (BansheePlayer *player, gboolean enabled)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    player->replaygain_enabled = enabled;
    bp_debug2 ("%s ReplayGain", enabled ? "Enabled" : "Disabled");
    _bp_replaygain_pipeline_rebuild (player);
}

P_INVOKE gboolean
bp_replaygain_get_enabled (BansheePlayer *player)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    return player->replaygain_enabled;
}
