//
// banshee-player-replaygain.c
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
bp_replaygain_db_to_linear(gdouble value)
{
    return pow(10, value / 20.0);
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
    if ((player->replaygain_enabled == TRUE) && (player->rgvolume != NULL)) {
    gdouble scale;

    g_object_get (G_OBJECT (player->rgvolume), "result-gain", &scale, NULL);

    bp_debug ("scaled volume: %.2f (ReplayGain) * %.2f (User) = %.2f",
            bp_replaygain_db_to_linear(scale), player->current_volume,
            bp_replaygain_db_to_linear(scale) * player->current_volume);
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
    bp_debug ("%s ReplayGain", enabled ? "Enabled" : "Disabled");
    _bp_pipeline_rebuild(player);
}

P_INVOKE gboolean
bp_replaygain_get_enabled (BansheePlayer *player)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    return player->replaygain_enabled;
}
