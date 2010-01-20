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
        public class Child
        {
            public Gdk.Rectangle Allocation { get; set; }
            public int ModelRowIndex { get; set; }
        }

        private List<Child> children = new List<Child> ();
        protected List<Child> Children {
            get { return children; }
        }

        protected ListViewBase View { get; set; }

        public Gdk.Rectangle ActualAllocation { get; protected set; }
        public Gdk.Size VirtualSize { get; protected set; }
        public Gdk.Size ChildSize { get; protected set; }
        public int XPosition { get; protected set; }
        public int YPosition { get; protected set; }
        public int ModelRowCount { get; protected set; }

        public int ChildCount {
            get { return Children.Count; }
        }

        public Child this[int index] {
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

        protected abstract void InvalidateChildSize ();
        protected abstract void InvalidateVirtualSize ();
        protected abstract void InvalidateChildCollection ();
        protected abstract void InvalidateChildLayout ();

        protected void ResizeChildCollection (int newChildCount)
        {
            int difference = Children.Count - newChildCount;
            while (Children.Count != newChildCount) {
                if (difference > 0) {
                    Children.RemoveAt (0);
                } else {
                    Children.Add (new Child ());
                }
            }
        }
    }
}
