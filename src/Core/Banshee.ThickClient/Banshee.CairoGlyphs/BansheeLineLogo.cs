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

        public static void Render (Context cr, double x, double y, double size)
        {
            Render (cr, x, y, size, inner_color, outer_color);
        }

        public static void Render (Context cr, double x, double y, double size, Color innerColor, Color outerColor)
        {
            double original_size = 12; // largest dimension as computed in rendering
            double scale = size / original_size;
            double pixel_align = Math.Round (scale / 2.0) + (Math.Floor (scale) % 2 == 0 ? 0 : 0.5);
            double tx = x - pixel_align;
            double ty = y - pixel_align;

            cr.Save ();
            cr.Translate (tx, ty);
            cr.Scale (scale, scale);

            cr.LineWidth = 1;
            cr.LineCap = LineCap.Round;
            cr.LineJoin = LineJoin.Round;

            // Inner B note
            cr.Color = innerColor;
            cr.MoveTo (1, 2);
            cr.LineTo (3, 0);
            cr.Arc (5, 8, 2, Math.PI, Math.PI * 3);
            cr.Stroke ();

            // Outer circle
            cr.Color = outerColor;
            cr.Arc (5, 8, 4, Math.PI * 1.5, Math.PI * 1.12);
            cr.Stroke ();

            cr.Restore ();
        }
    }
}
