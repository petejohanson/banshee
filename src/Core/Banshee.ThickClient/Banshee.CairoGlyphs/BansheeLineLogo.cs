//
// BansheeLineLogo.cs
// 
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2009 Novell, Inc.
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

using System;
using Cairo;

namespace Banshee.CairoGlyphs
{
    public static class BansheeLineLogo
    {
        private static Color inner_color = Hyena.Gui.CairoExtensions.RgbaToColor (0xddddddff);
        private static Color outer_color = Hyena.Gui.CairoExtensions.RgbaToColor (0xddddddff);

        public static void Render (Context cr, Rectangle box)
        {
            Render (cr, box, inner_color, outer_color);
        }

        public static void Render (Context cr, Rectangle box, Color innerColor, Color outerColor)
        {
            Render (cr, box, 0.5, 0.5, innerColor, outerColor);
        }

        public static void Render (Context cr, Rectangle box,
            double xalign, double yalign, Color innerColor, Color outerColor)
        {
            // virtual size of the figure we will draw below on the 1:1 scale
            double original_width = 8;
            double original_height = 12;

            // figure out the scale dimensions of the bounding box and the glyph
            double box_size = Math.Min (box.Width, box.Height);
            double original_size = Math.Max (original_width, original_height);

            // create a scale for the box (ratio between virtual glyph size and the box),
            // then re-scale to account for the extra size that will be added via stroke.
            // glyph_scale is the stroke width and the actual transformation size
            double box_scale = box_size / original_size;
            double glyph_scale = Math.Floor ((box_size - box_scale) / original_size);

            // compute the alignment to the pixel grid for the stroke/scale
            double pixel_align = Math.Floor (glyph_scale + 0.5) / 2.0;

            // figure out the actual size in pixels of the glyph
            double actual_width = glyph_scale * original_width + 2 * Math.Floor (pixel_align);
            double actual_height = glyph_scale * original_height + 2 * Math.Floor (pixel_align);

            // compute the offset accounting for box, grid alignment, and figure alignment
            double tx = box.X + pixel_align + Math.Round ((box.Width - actual_width) * xalign);
            double ty = box.Y + pixel_align + Math.Round ((box.Height - actual_height) * yalign);

            // save the context, and transform the current/new context
            cr.Save ();
            cr.Translate (tx, ty);
            cr.Scale (glyph_scale, glyph_scale);

            // define how the strokes look
            cr.LineWidth = 1;
            cr.LineCap = LineCap.Round;
            cr.LineJoin = LineJoin.Round;

            // inner 'b' note
            cr.Color = innerColor;
            cr.MoveTo (0, 2);
            cr.LineTo (2, 0);
            cr.Arc (4, 8, 2, Math.PI, Math.PI * 3);
            cr.Stroke ();

            // outer 'cut' circle
            cr.Color = outerColor;
            cr.Arc (4, 8, 4, Math.PI * 1.5, Math.PI * 1.12);
            cr.Stroke ();

            cr.Restore ();
        }
    }
}
