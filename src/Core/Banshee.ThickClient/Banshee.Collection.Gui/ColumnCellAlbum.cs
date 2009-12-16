//
// ColumnCellAlbum.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using Gtk;
using Cairo;

using Hyena.Gui;
using Hyena.Gui.Theming;
using Hyena.Data.Gui;
using Hyena.Data.Gui.Accessibility;

using Banshee.Gui;
using Banshee.ServiceStack;

namespace Banshee.Collection.Gui
{
    public class ColumnCellAlbum : ColumnCell
    {
        private static int image_spacing = 4;
        private static int image_size = 48;

        private ArtworkManager artwork_manager;

        public ColumnCellAlbum () : base (null, true)
        {
            artwork_manager = ServiceManager.Get<ArtworkManager> ();
        }

        private class ColumnCellAlbumAccessible : ColumnCellAccessible
        {
            public ColumnCellAlbumAccessible (object bound_object, ColumnCellAlbum cell, ICellAccessibleParent parent)
                : base (bound_object, cell as ColumnCell, parent)
            {
                AlbumInfo bound_album_info = (AlbumInfo)bound_object;
                Name = String.Format ("{0} - {1}",
                                     bound_album_info.DisplayTitle,
                                     bound_album_info.DisplayArtistName);
            }
        }

        public override Atk.Object GetAccessible (ICellAccessibleParent parent)
        {
            return new ColumnCellAlbumAccessible (BoundObject, this, parent);
        }

        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight)
        {
            if (BoundObject == null) {
                return;
            }

            if (!(BoundObject is AlbumInfo)) {
                throw new InvalidCastException ("ColumnCellAlbum can only bind to AlbumInfo objects");
            }

            AlbumInfo album = (AlbumInfo)BoundObject;

            int actual_image_size = LayoutStyle == DataViewLayoutStyle.Grid
                ? (int)Math.Max (Math.Min (cellWidth, cellHeight) - 10, 0)
                : image_size;
            ImageSurface image = artwork_manager == null ? null
                : artwork_manager.LookupScaleSurface (album.ArtworkId, actual_image_size, true);

            // int image_render_size = is_default ? image.Height : (int)cellHeight - 8;
            int image_render_size = actual_image_size;
            int x = LayoutStyle == DataViewLayoutStyle.Grid
                ? ((int)cellWidth - image_render_size) / 2
                : image_spacing;
            int y = ((int)cellHeight - image_render_size) / 2;

            ArtworkRenderer.RenderThumbnail (context.Context, image, false, x, y,
                image_render_size, image_render_size, true, context.Theme.Context.Radius,
                image == null, new Color (0.8, 0.8, 0.8));

            if (LayoutStyle == DataViewLayoutStyle.Grid) {
                return;
            }

            int fl_width = 0, fl_height = 0, sl_width = 0, sl_height = 0;
            Cairo.Color text_color = context.Theme.Colors.GetWidgetColor (GtkColorClass.Text, state);
            text_color.A = 0.75;

            Pango.Layout layout = context.Layout;
            layout.Width = (int)((cellWidth - cellHeight - x - 10) * Pango.Scale.PangoScale);
            layout.Ellipsize = Pango.EllipsizeMode.End;
            layout.FontDescription.Weight = Pango.Weight.Bold;

            // Compute the layout sizes for both lines for centering on the cell
            int old_size = layout.FontDescription.Size;

            layout.SetText (album.DisplayTitle);
            layout.GetPixelSize (out fl_width, out fl_height);

            if (!String.IsNullOrEmpty (album.ArtistName)) {
                layout.FontDescription.Weight = Pango.Weight.Normal;
                layout.FontDescription.Size = (int)(old_size * Pango.Scale.Small);
                layout.FontDescription.Style = Pango.Style.Italic;
                layout.SetText (album.ArtistName);
                layout.GetPixelSize (out sl_width, out sl_height);
            }

            // Calculate the layout positioning
            x = ((int)cellHeight - x) + 10;
            y = (int)((cellHeight - (fl_height + sl_height)) / 2);

            // Render the second line first since we have that state already
            if (!String.IsNullOrEmpty (album.ArtistName)) {
                context.Context.MoveTo (x, y + fl_height);
                context.Context.Color = text_color;
                PangoCairoHelper.ShowLayout (context.Context, layout);
            }

            // Render the first line, resetting the state
            layout.SetText (album.DisplayTitle);
            layout.FontDescription.Weight = Pango.Weight.Bold;
            layout.FontDescription.Size = old_size;
            layout.FontDescription.Style = Pango.Style.Normal;

            layout.SetText (album.DisplayTitle);

            context.Context.MoveTo (x, y);
            text_color.A = 1;
            context.Context.Color = text_color;
            PangoCairoHelper.ShowLayout (context.Context, layout);
        }

        public override Gdk.Size Measure (Widget widget)
        {
            int text_height = 0;

            using (var layout = new Pango.Layout (widget.PangoContext) {
                FontDescription = widget.PangoContext.FontDescription.Copy () }) {

                layout.FontDescription.Weight = Pango.Weight.Bold;
                text_height = layout.FontDescription.MeasureTextHeight (widget.PangoContext);

                layout.FontDescription.Weight = Pango.Weight.Normal;
                layout.FontDescription.Size = (int)(layout.FontDescription.Size * Pango.Scale.Small);
                layout.FontDescription.Style = Pango.Style.Italic;
                text_height += layout.FontDescription.MeasureTextHeight (widget.PangoContext);
            }

            return LayoutStyle == DataViewLayoutStyle.Grid
                ? new Gdk.Size (100, 100 + text_height + 6)
                : new Gdk.Size (0, (text_height < image_size ? image_size : text_height) + 6);
        }
    }
}
