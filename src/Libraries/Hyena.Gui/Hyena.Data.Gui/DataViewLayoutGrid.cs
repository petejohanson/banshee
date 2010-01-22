//
// DataViewLayoutGrid.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2010 Novell, Inc.
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
using System.Collections.Generic;

namespace Hyena.Data.Gui
{
    public class DataViewLayoutGrid : DataViewLayout
    {
        public int Rows { get; private set; }
        public int Columns { get; private set; }

        protected override void InvalidateChildSize ()
        {
            ChildSize = new Gdk.Size (48, 48);
        }

        protected override void InvalidateVirtualSize ()
        {
            // FIXME: this is too simplistic and can result in
            // an extra row of allocation, check the model size
            VirtualSize = new Gdk.Size (
                ChildSize.Width * Math.Max (Columns, 1),
                (ChildSize.Height * ModelRowCount) / Math.Max (Rows, 1));
        }

        protected override void InvalidateChildCollection ()
        {
            Rows = ChildSize.Height > 0
                ? (int)Math.Ceiling ((ActualAllocation.Height +
                    ChildSize.Height) / (double)ChildSize.Height)
                : 0;
            Columns = ChildSize.Width > 0
                ? (int)Math.Max (ActualAllocation.Width / ChildSize.Width, 1)
                : 0;

            ResizeChildCollection (Rows * Columns);
        }

        protected override void InvalidateChildLayout ()
        {
            if (ChildSize.Width <= 0 || ChildSize.Height <= 0) {
                // FIXME: empty/reset all child slots here?
                return;
            }

            // Compute where we should start and end in the model
            int offset = ActualAllocation.Y - YPosition % ChildSize.Height;
            int first_model_row = (int)Math.Floor (YPosition / (double)ChildSize.Height) * Columns;
            int last_model_row = first_model_row + Rows * Columns;

            // Setup for the layout iteration
            int model_row_index = first_model_row;
            int layout_child_index = 0;
            int view_row_index = 0;
            int view_column_index = 0;

            // Allocation of the first child in the layout, this
            // will change as we iterate the layout children
            var child_allocation = new Gdk.Rectangle () {
                X = ActualAllocation.X,
                Y = offset,
                Width = ChildSize.Width,
                Height = ChildSize.Height
            };

            // Iterate the layout children and configure them for the current
            // view state to be consumed by interaction and rendering phases
            for (; model_row_index < last_model_row; model_row_index++, layout_child_index++) {
                var child = Children[layout_child_index];
                child.Allocation = child_allocation;
                child.VirtualAllocation = GetChildVirtualAllocation (child_allocation);
                child.ModelRowIndex = model_row_index;

                // Update the allocation for the next child
                if (++view_column_index % Columns == 0) {
                    view_row_index++;
                    view_column_index = 0;

                    child_allocation.Y += ChildSize.Height;
                    child_allocation.X = ActualAllocation.X;
                } else {
                    child_allocation.X += ChildSize.Width;
                }

                // FIXME: clear any layout children that go beyond the model
            }
        }
    }
}
