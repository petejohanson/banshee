//
// LazyLoadSourceContents.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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

using Banshee.Sources;

namespace Banshee.Sources.Gui
{
    public class LazyLoadSourceContents<T> : ISourceContents, IDisposable where T : ISourceContents
    {
        private object [] args;
        private T actual_contents;
        private ISourceContents ActualContents {
            get {
                if (actual_contents == null) {
                    lock (this) {
                        if (actual_contents == null) {
                            actual_contents = (T) Activator.CreateInstance (typeof(T), args);
                        }
                    }
                }

                return actual_contents;
            }
        }

        public LazyLoadSourceContents (params object [] args)
        {
            this.args = args;
        }

        public void Dispose ()
        {
            var disposable = actual_contents as IDisposable;
            if (disposable != null) {
                disposable.Dispose ();
            }
        }

        public bool SetSource (ISource source)
        {
            return ActualContents.SetSource (source);
        }

        public void ResetSource ()
        {
            ActualContents.ResetSource ();
        }

        public T Contents {
            get { return actual_contents; }
        }

        public ISource Source {
            get { return ActualContents.Source; }
        }

        public Widget Widget {
            get { return ActualContents.Widget; }
        }
    }
}
