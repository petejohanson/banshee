//
// JsonObject.cs
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

using System;
using System.Collections.Generic;
using System.Text;

namespace Hyena.Json
{
    public class JsonObject : Dictionary<string, object>, IJsonCollection
    {
        public void Dump ()
        {
            Dump (1);
        }

        public void Dump (int level)
        {
            Console.Write (ToString (level));
        }

        public override string ToString ()
        {
            return ToString (1);
        }

        public string ToString (int level)
        {
            var sb = new StringBuilder ();
            Dump (sb, level);
            return sb.ToString ();
        }

        public void Dump (StringBuilder sb, int level)
        {
            if (Count == 0) {
                sb.AppendLine ("{ }");
                return;
            }

            sb.AppendLine ("{");
            foreach (KeyValuePair<string, object> item in this) {
                sb.AppendFormat ("{0}\"{1}\" : ", String.Empty.PadLeft (level * 2, ' '), item.Key);
                if (item.Value is IJsonCollection) {
                    ((IJsonCollection)item.Value).Dump (sb, level + 1);
                } else {
                    sb.AppendLine (item.Value.ToString ());
                }
            }
            sb.AppendFormat ("{0}}}\n", String.Empty.PadLeft ((level - 1) * 2, ' '));
        }
    }
}
