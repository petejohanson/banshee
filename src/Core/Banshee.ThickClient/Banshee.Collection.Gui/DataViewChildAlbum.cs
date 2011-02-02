//
// DataViewChildAlbum.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2007-2010 Novell, Inc.
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
using Hyena.Gui.Canvas;
using Hyena.Data.Gui;
using Hyena.Data.Gui.Accessibility;
using Hyena.Gui.Theatrics;

using Banshee.Gui;
using Banshee.ServiceStack;

namespace Banshee.Collection.Gui
{
    public class DataViewChildAlbum : StackPanel
    {
        DataViewChildImage img;

        public DataViewChildAlbum ()
        {
            Orientation = Hyena.Gui.Canvas.Orientation.Vertical;
            Margin = new Thickness (5);
            Width = 90;
            Spacing = 2;

            Children.Add (img = new DataViewChildImage ());
            Children.Add (
                new TextBlock () {
                    Binder = new Hyena.Data.ObjectBinder () { Property = "DisplayTitle" },
                    UseMarkup = true,
                    TextFormat = "<small>{0}</small>",
                }
            );
            Children.Add (
                new TextBlock () {
                    Binder = new Hyena.Data.ObjectBinder () { Property = "DisplayArtistName" },
                    UseMarkup = true,
                    // TODO hide for 'All Artists'
                    TextFormat = "<small>{0}</small>",
                    Opacity = 0.6
                }
            );

            // Render the prelight just on the cover art, but triggered by being anywhere over the album
            PrelightRenderer = (cr, theme, size, o) => Prelight.Gradient (cr, theme, img.ContentAllocation, o);
        }

        public double ImageSize {
            get { return img.ImageSize; }
            set {
                Width = img.ImageSize = value;
            }
        }

#if false
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
#endif

    }

    public class DataViewChildImage : CanvasItem
    {
        private ArtworkManager artwork_manager;

        public double ImageSize { get; set; }

        public DataViewChildImage ()
        {
            Binder = new Hyena.Data.ObjectBinder () { Property = "ArtworkId" };
            artwork_manager = ServiceManager.Get<ArtworkManager> ();
            ImageSize = 90;
        }

        public override Size Measure (Size available)
        {
            Width = Height = ImageSize;
            return DesiredSize = new Size (Width + Margin.X, Height + Margin.Y);
        }

        public override void Arrange ()
        {
        }

        protected override void ClippedRender (CellContext context)
        {
            var artwork_id = BoundObject as string;
            var image_surface = artwork_id != null && artwork_manager != null
                ? artwork_manager.LookupScaleSurface (artwork_id, (int)ImageSize, true)
                : null;

            ArtworkRenderer.RenderThumbnail (context.Context,
                image_surface, false,
                0, 0, ImageSize, ImageSize,
                true, context.Theme.Context.Radius,
                image_surface == null, new Color (0.8, 0.8, 0.8)
            );
        }
    }
}
