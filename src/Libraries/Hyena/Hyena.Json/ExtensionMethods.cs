//
// ExtensionMethods.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (c) 2010 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Hyena.Json
{
    public static class ExtensionMethods
    {
        // JsonObject serializer
        public static string ToJsonString (this Dictionary<string, object> obj)
        {
            return obj.ToJsonString (1);
        }

        public static string ToJsonString (this Dictionary<string, object> obj, int level)
        {
            var sb = new StringBuilder ();
            obj.ToJsonString (sb, level);
            return sb.ToString ();
        }

        public static void ToJsonString (this Dictionary<string, object> obj, StringBuilder sb, int level)
        {
            if (obj.Count == 0) {
                sb.AppendLine ("{ }");
                return;
            }

            sb.AppendLine ("{");
            foreach (KeyValuePair<string, object> item in obj) {
                sb.AppendFormat ("{0}\"{1}\" : ", String.Empty.PadLeft (level * 2, ' '), item.Key);
                item.Value.ToJsonString (sb, level + 1);
            }
            sb.AppendFormat ("{0}}}\n", String.Empty.PadLeft ((level - 1) * 2, ' '));
        }

        // System.Linq.IGrouping serializer
        public static string ToJsonString<K, V> (this IEnumerable<IGrouping<K, V>> obj)
        {
            return obj.ToJsonString (1);
        }

        public static string ToJsonString<K, V> (this IEnumerable<IGrouping<K, V>> obj, int level)
        {
            var sb = new StringBuilder ();
            obj.ToJsonString (sb, level);
            return sb.ToString ();
        }

        public static void ToJsonString<K, V> (this IEnumerable<IGrouping<K, V>> obj, StringBuilder sb, int level)
        {
            bool first = true;

            sb.Append ("{");
            foreach (var item in obj) {
                if (first) {
                    first = false;
                    sb.AppendLine ();
                }
                sb.AppendFormat ("{0}\"{1}\" : ", String.Empty.PadLeft (level * 2, ' '), item.Key);
                item.ToJsonString (sb, level + 1);
            }
            sb.AppendFormat ("{0}}}\n", first ? " " : String.Empty.PadLeft ((level - 1) * 2, ' '));
        }

        // JsonArray serializer
        public static string ToJsonString (this IEnumerable list)
        {
            return list.ToJsonString (1);
        }

        public static string ToJsonString (this IEnumerable list, int level)
        {
            var sb = new StringBuilder ();
            list.ToJsonString (sb, level);
            return sb.ToString ();
        }

        public static void ToJsonString (this IEnumerable list, StringBuilder sb, int level)
        {
            bool first = true;
            sb.Append ("[");
            foreach (object item in list) {
                if (first) {
                    first = false;
                    sb.AppendLine ();
                }

                sb.Append (String.Empty.PadLeft (level * 2, ' '));
                item.ToJsonString (sb, level + 1);
            }

            sb.AppendFormat ("{0}]\n", first ? " " : String.Empty.PadLeft ((level - 1) * 2, ' '));
        }

        // Utility method
        private static void ToJsonString (this object item, StringBuilder sb, int level)
        {
            if (item is Dictionary<string, object>) {
                ((Dictionary<string, object>)item).ToJsonString (sb, level);
            //} else if (item is IGrouping) {
                //((IGrouping)item).ToJsonString (sb, level);
            } else if (item is IEnumerable && !(item is string)) {
                ((IEnumerable)item).ToJsonString (sb, level);
            } else {
                sb.AppendLine (item.ToString ());
            }
        }
    }
}
