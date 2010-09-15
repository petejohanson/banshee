//
// banshee-player.c
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Julien Moutte <julien@fluendo.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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

#include "banshee-player-private.h"
#include "banshee-player-pipeline.h"
#include "banshee-player-cdda.h"
#include "banshee-player-missing-elements.h"
#include "banshee-player-replaygain.h"

// ---------------------------------------------------------------------------
// Private Functions
// ---------------------------------------------------------------------------

static void
bp_pipeline_set_state (BansheePlayer *player, GstState state)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (GST_IS_ELEMENT (player->playbin)) {
        player->target_state = state;
        gst_element_set_state (player->playbin, state);
    }
}

// ---------------------------------------------------------------------------
// Public Functions
// ---------------------------------------------------------------------------

P_INVOKE void
bp_destroy (BansheePlayer *player)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (player->video_mutex != NULL) {
        g_mutex_free (player->video_mutex);
    }

    if (player->replaygain_mutex != NULL) {
        g_mutex_free (player->replaygain_mutex);
    }
    
    if (player->cdda_device != NULL) {
        g_free (player->cdda_device);
    }
    
    _bp_pipeline_destroy (player);
    _bp_missing_elements_destroy (player);
    
    memset (player, 0, sizeof (BansheePlayer));
    
    g_free (player);
    player = NULL;
    
    bp_debug ("bp_destroy: disposed player");
}

P_INVOKE BansheePlayer *
bp_new ()
{
    BansheePlayer *player = g_new0 (BansheePlayer, 1);
    
    player->video_mutex = g_mutex_new ();
    player->replaygain_mutex = g_mutex_new ();

    return player;
}

P_INVOKE gboolean
bp_initialize_pipeline (BansheePlayer *player)
{
    return _bp_pipeline_construct (player);
}

P_INVOKE gboolean
bp_open (BansheePlayer *player, const gchar *uri)
{
    GstState state;
    
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    
    // Build the pipeline if we need to
    if (player->playbin == NULL && !_bp_pipeline_construct (player)) {
        return FALSE;
    }

    // Give the CDDA code a chance to intercept the open request
    // in case it is able to perform a fast seek to a track
    if (_bp_cdda_handle_uri (player, uri)) {
        return TRUE;
    } else if (player->playbin == NULL) {
        return FALSE;
    }
    
    // Set the pipeline to the proper state
    gst_element_get_state (player->playbin, &state, NULL, 0);
    if (state >= GST_STATE_PAUSED) {
        player->target_state = GST_STATE_READY;
        gst_element_set_state (player->playbin, GST_STATE_READY);
    }
    
    // Pass the request off to playbin
    g_object_set (G_OBJECT (player->playbin), "uri", uri, NULL);
    
    player->in_gapless_transition = FALSE;
    
    return TRUE;
}

P_INVOKE void
bp_stop (BansheePlayer *player, gboolean nullstate)
{
    // Some times "stop" really means "pause", particularly with
    // CDDA track transitioning; a NULL state will release resources
    GstState state = nullstate ? GST_STATE_NULL : GST_STATE_PAUSED;
    
    if (!nullstate && player->cdda_device == NULL) {
        // only allow going to PAUSED if we're playing CDDA
        state = GST_STATE_NULL;
    }
    
    bp_debug2 ("bp_stop: setting state to %s",
        state == GST_STATE_NULL ? "GST_STATE_NULL" : "GST_STATE_PAUSED");
    
    player->in_gapless_transition = FALSE;
    
    bp_pipeline_set_state (player, state);
}

P_INVOKE void
bp_pause (BansheePlayer *player)
{
    bp_pipeline_set_state (player, GST_STATE_PAUSED);
}

P_INVOKE void
bp_play (BansheePlayer *player)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    bp_pipeline_set_state (player, GST_STATE_PLAYING);
}

P_INVOKE gboolean
bp_set_next_track (BansheePlayer *player, const gchar *uri)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    g_return_val_if_fail (player->playbin != NULL, FALSE);
    g_object_set (G_OBJECT (player->playbin), "uri", uri, NULL);
    return TRUE;
}

P_INVOKE gboolean
bp_set_position (BansheePlayer *player, guint64 time_ms)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    
    if (player->playbin == NULL || !gst_element_seek (player->playbin, 1.0, 
        GST_FORMAT_TIME, GST_SEEK_FLAG_FLUSH,
        GST_SEEK_TYPE_SET, time_ms * GST_MSECOND, 
        GST_SEEK_TYPE_NONE, GST_CLOCK_TIME_NONE)) {
        g_warning ("Could not seek in stream");
        return FALSE;
    }
    
    return TRUE;
}

P_INVOKE guint64
bp_get_position (BansheePlayer *player)
{
    static GstFormat format = GST_FORMAT_TIME;
    gint64 position;

    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), 0);

    if (player->playbin != NULL && gst_element_query_position (player->playbin, &format, &position)) {
        return position / GST_MSECOND;
    }
    
    return 0;
}

P_INVOKE guint64
bp_get_duration (BansheePlayer *player)
{
    static GstFormat format = GST_FORMAT_TIME;
    gint64 duration;

    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), 0);

    if (player->playbin != NULL && gst_element_query_duration (player->playbin, &format, &duration)) {
        return duration / GST_MSECOND;
    }
    
    return 0;
}

P_INVOKE gboolean
bp_can_seek (BansheePlayer *player)
{
    GstQuery *query;
    gboolean can_seek = TRUE;
    
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    
    if (player->playbin == NULL) {
        return FALSE;
    }
    
    query = gst_query_new_seeking (GST_FORMAT_TIME);
    if (!gst_element_query (player->playbin, query)) {
        // This will probably fail, 100% of the time, because it's apparently 
        // very unimplemented in GStreamer... when it's fixed
        // we will return FALSE here, and show the warning
        // g_warning ("Could not query pipeline for seek ability");
        return bp_get_duration (player) > 0;
    }
    
    gst_query_parse_seeking (query, NULL, &can_seek, NULL, NULL);
    gst_query_unref (query);
    
    return can_seek && bp_get_duration (player) > 0;
}

P_INVOKE gboolean
bp_supports_gapless (BansheePlayer *player)
{
#ifdef ENABLE_GAPLESS
    return TRUE;
#else
    return FALSE;
#endif //ENABLE_GAPLESS
}

P_INVOKE gboolean
bp_supports_stream_volume (BansheePlayer *player)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    return player->supports_stream_volume;
}

P_INVOKE gboolean
bp_audiosink_has_volume (BansheePlayer *player)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    return player->audiosink_has_volume;
}

P_INVOKE void
bp_set_volume (BansheePlayer *player, gdouble volume)
{
    GParamSpec *volume_spec;
    GValue value = { 0, };

    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    g_return_if_fail (GST_IS_ELEMENT(player->volume));

    player->current_volume = CLAMP (volume, 0.0, 1.0);
    volume_spec = g_object_class_find_property (G_OBJECT_GET_CLASS (player->volume), "volume");
    g_value_init (&value, G_TYPE_DOUBLE);
    g_value_set_double (&value, player->current_volume);
    g_param_value_validate (volume_spec, &value);

    g_object_set_property (G_OBJECT (player->volume), "volume", &value);
    g_value_unset (&value);
    _bp_rgvolume_print_volume(player);
}

P_INVOKE gdouble
bp_get_volume (BansheePlayer *player)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), 0.0);
    return player->current_volume;
}

P_INVOKE void
bp_set_volume_changed_callback (BansheePlayer *player, BansheePlayerVolumeChangedCallback cb)
{
    SET_CALLBACK (volume_changed_cb);
}

P_INVOKE gboolean
bp_get_pipeline_elements (BansheePlayer *player, GstElement **playbin, GstElement **audiobin, GstElement **audiotee)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    
    *playbin = player->playbin;
    *audiobin = player->audiobin;
    *audiotee = player->audiotee;
    
    return TRUE;
}

P_INVOKE void
bp_set_application_gdk_window(BansheePlayer *player, GdkWindow *window)
{
    player->window = window;
}

P_INVOKE void
bp_set_eos_callback (BansheePlayer *player, BansheePlayerEosCallback cb)
{
    SET_CALLBACK(eos_cb);
}

P_INVOKE void
bp_set_error_callback (BansheePlayer *player, BansheePlayerErrorCallback cb)
{
    SET_CALLBACK (error_cb);
}

P_INVOKE void
bp_set_state_changed_callback (BansheePlayer *player, BansheePlayerStateChangedCallback cb)
{
    SET_CALLBACK (state_changed_cb);
}

P_INVOKE void
bp_set_buffering_callback (BansheePlayer *player, BansheePlayerBufferingCallback cb)
{
    SET_CALLBACK(buffering_cb);
}

P_INVOKE void
bp_set_tag_found_callback (BansheePlayer *player, BansheePlayerTagFoundCallback cb)
{
    SET_CALLBACK (tag_found_cb);
}

P_INVOKE void
bp_get_error_quarks (GQuark *core, GQuark *library, GQuark *resource, GQuark *stream)
{
    *core = GST_CORE_ERROR;
    *library = GST_LIBRARY_ERROR;
    *resource = GST_RESOURCE_ERROR;
    *stream = GST_STREAM_ERROR;
}

P_INVOKE void
bp_set_next_track_starting_callback (BansheePlayer *player, BansheePlayerNextTrackStartingCallback cb)
{
    SET_CALLBACK (next_track_starting_cb);
}

P_INVOKE void
bp_set_about_to_finish_callback (BansheePlayer *player, BansheePlayerAboutToFinishCallback cb)
{
    SET_CALLBACK (about_to_finish_cb);
}
