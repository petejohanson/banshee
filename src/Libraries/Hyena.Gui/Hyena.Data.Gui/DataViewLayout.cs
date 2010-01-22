//
// DataViewLayout.cs
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
    public abstract class DataViewLayout
    {
        private List<DataViewChild> children = new List<DataViewChild> ();
        protected List<DataViewChild> Children {
            get { return children; }
        }

        public ListViewBase View { get; set; }

        public Gdk.Rectangle ActualAllocation { get; protected set; }
        public Gdk.Size VirtualSize { get; protected set; }
        public Gdk.Size ChildSize { get; protected set; }
        public int XPosition { get; protected set; }
        public int YPosition { get; protected set; }
        public int ModelRowCount { get; protected set; }

        public int ChildCount {
            get { return Children.Count; }
        }

        public DataViewChild this[int index] {
            get { return Children[index]; }
        }

        public void UpdatePosition (int x, int y)
        {
            XPosition = x;
            YPosition = y;
            InvalidateChildLayout ();
        }

        public void UpdateModelRowCount (int modelRowCount)
        {
            ModelRowCount = modelRowCount;
            InvalidateVirtualSize ();
        }

        public virtual void Allocate (Gdk.Rectangle actualAllocation)
        {
            ActualAllocation = actualAllocation;

            InvalidateChildSize ();
            InvalidateChildCollection ();
            InvalidateChildLayout ();
        }

        public virtual DataViewChild FindChildAtPoint (int x, int y)
        {
            return Children.Find (child => child.Allocation.Contains (
                ActualAllocation.X + x, ActualAllocation.Y + y));
        }

        public virtual DataViewChild FindChildAtModelRowIndex (int modelRowIndex)
        {
            return Children.Find (child => child.ModelRowIndex == modelRowIndex);
        }

        protected abstract void InvalidateChildSize ();
        protected abstract void InvalidateVirtualSize ();
        protected abstract void InvalidateChildCollection ();
        protected abstract void InvalidateChildLayout ();

        protected Gdk.Rectangle GetChildVirtualAllocation (Gdk.Rectangle childAllocation)
        {
            return new Gdk.Rectangle () {
                X = childAllocation.X - ActualAllocation.X,
                Y = childAllocation.Y - ActualAllocation.Y,
                Width = childAllocation.Width,
                Height = childAllocation.Height
            };
        }
    }
}
