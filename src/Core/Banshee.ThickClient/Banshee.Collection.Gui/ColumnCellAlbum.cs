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
    public class ColumnCellAlbum : ColumnCell, IInteractiveCell
    {
        private ArtworkManager artwork_manager;
        private object hover_object;

        public double PaddingX { get; set; }
        public double PaddingY { get; set; }
        public double ImageSize { get; set; }
        public double ImageSpacing { get; set; }
        public double TextSpacing { get; set; }

        private bool IsGridLayout {
            // FIXME: Cache this after implementing virtual notification
            // on ColumnCell that ViewLayout has changed ...
            get { return ViewLayout is AlbumViewGridLayout; }
        }

        public ColumnCellAlbum () : base (null, true)
        {
            artwork_manager = ServiceManager.Get<ArtworkManager> ();

            PaddingX = PaddingY = 6;
            ImageSize = 48;
            ImageSpacing = 4;
            TextSpacing = 0;

            PaddingX = PaddingY = 5;
            ImageSize = 90;
            ImageSpacing = 2;
            TextSpacing = -2;
        }

        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight)
        {
            double x = 0;
            double y = 0;
            double width = ImageSize;
            double height = ImageSize;

            ImageSurface image_surface;
            string [] lines;

            if (!HandleBoundObject (out image_surface, out lines)) {
                return;
            }

            if (PaddingX > 0 || PaddingY > 0) {
                context.Context.Translate (PaddingX, PaddingY);
            }

            // Render the image
            if (image_surface != null) {
                width = image_surface.Width;
                height = image_surface.Height;
            }

            if (IsGridLayout) {
                x = Math.Round ((cellWidth - 2 * PaddingX - width) / 2.0);
            } else {
                y = Math.Round ((cellHeight - 2 * PaddingY - height) / 2.0);
            }

            RenderImageSurface (context, new Rectangle (x, y, width, height), image_surface);

            // Render the overlay
            if (IsGridLayout && hover_object != null) {
                var cr = context.Context;
                cr.Color = new Color (0, 0, 0, 0.5);
                cr.Rectangle (x, y, width, height);
                cr.Fill ();
            }

            if (lines == null || lines.Length < 2) {
                return;
            }

            // Render the text
            int fl_width = 0, fl_height = 0, sl_width = 0, sl_height = 0;
            Cairo.Color text_color = context.Theme.Colors.GetWidgetColor (GtkColorClass.Text, state);
            text_color.A = 0.75;

            var layout = context.Layout;
            layout.Ellipsize = Pango.EllipsizeMode.End;
            layout.FontDescription.Weight = Pango.Weight.Bold;

            layout.Width = (int)((IsGridLayout
                ? cellWidth - 2 * PaddingX
                : cellWidth - ImageSize - ImageSpacing - 2 * PaddingX) * Pango.Scale.PangoScale);

            // Compute the layout sizes for both lines for centering on the cell
            int old_size = layout.FontDescription.Size;

            layout.SetText (lines[1]);
            layout.GetPixelSize (out fl_width, out fl_height);

            if (!String.IsNullOrEmpty (lines[1])) {
                layout.FontDescription.Weight = Pango.Weight.Normal;
                layout.FontDescription.Size = (int)(old_size * Pango.Scale.Small);
                layout.FontDescription.Style = Pango.Style.Italic;
                layout.SetText (lines[1]);
                layout.GetPixelSize (out sl_width, out sl_height);
            }

            if (IsGridLayout) {
                x = 0;
                y = ImageSize + ImageSpacing;
            } else {
                x = ImageSize + ImageSpacing;
                y = Math.Round ((cellHeight - fl_height + sl_height) / 2);
            }

            // Render the second line first since we have that state already
            if (!String.IsNullOrEmpty (lines[0])) {
                context.Context.MoveTo (x, y + fl_height + TextSpacing);
                context.Context.Color = text_color;
                PangoCairoHelper.ShowLayout (context.Context, layout);
            }

            // Render the first line, resetting the state
            layout.SetText (lines[0]);
            layout.FontDescription.Weight = Pango.Weight.Bold;
            layout.FontDescription.Size = old_size;
            layout.FontDescription.Style = Pango.Style.Normal;

            layout.SetText (lines[0]);

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

            double width, height;

            if (ViewLayout is AlbumViewGridLayout) {
                width = ImageSize + 2 * PaddingX;
                height = ImageSize + ImageSpacing + TextSpacing + text_height + 2 * PaddingY;
            } else {
                double list_text_height = text_height + TextSpacing;
                width = ImageSize + ImageSpacing + 2 * PaddingY;
                height = (list_text_height < ImageSize ? ImageSize : list_text_height) + 2 * PaddingX;
            }

            return new Gdk.Size ((int)Math.Round (width), (int)Math.Round (height));
        }

        protected virtual void RenderImageSurface (CellContext context, Rectangle allocation, ImageSurface imageSurface)
        {
            ArtworkRenderer.RenderThumbnail (context.Context,
                imageSurface, false,
                allocation.X, allocation.Y, allocation.Width, allocation.Height,
                true, context.Theme.Context.Radius,
                imageSurface == null, new Color (0.8, 0.8, 0.8));
        }

        protected virtual bool HandleBoundObject (out ImageSurface image, out string [] lines)
        {
            image = null;
            lines = null;

            var album = BoundObject as AlbumInfo;
            if (album == null) {
                if (BoundObject == null) {
                    return false;
                };

                throw new InvalidCastException ("ColumnCellAlbum can only bind to AlbumInfo objects");
            }

            lines = new [] { album.DisplayTitle, album.DisplayArtistName };
            image = artwork_manager != null
                ? artwork_manager.LookupScaleSurface (album.ArtworkId, (int)ImageSize, true)
                : null;

            return true;
        }

#region IInteractiveCell

        public bool ButtonEvent (int x, int y, bool pressed, Gdk.EventButton evnt)
        {
            return false;
        }

        public bool MotionEvent (int x, int y, Gdk.EventMotion evnt)
        {
            var redraw = hover_object != BoundObject;
            hover_object = BoundObject;
            return redraw;
        }

        public bool PointerLeaveEvent ()
        {
            hover_object = null;
            return true;
        }

#endregion

#region Accessibility

        private class ColumnCellAlbumAccessible : ColumnCellAccessible
        {
            public ColumnCellAlbumAccessible (object bound_object, ColumnCellAlbum cell, ICellAccessibleParent parent)
                : base (bound_object, cell as ColumnCell, parent)
            {
                var bound_album_info = (AlbumInfo)bound_object;
                Name = String.Format ("{0} - {1}", bound_album_info.DisplayTitle, bound_album_info.DisplayArtistName);
            }
        }

        public override Atk.Object GetAccessible (ICellAccessibleParent parent)
        {
            return new ColumnCellAlbumAccessible (BoundObject, this, parent);
        }

#endregion

    }
}
