//
// IConfigurationClient.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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

namespace Banshee.Configuration
{
    public interface IConfigurationClient
    {
        bool TryGet<T> (string @namespace, string key, out T result);
        void Set<T> (string @namespace, string key, T value);
    }

    public static class Extensions
    {
        public static T Get<T> (this IConfigurationClient client, SchemaEntry<T> entry)
        {
            return client.Get<T> (entry.Namespace, entry.Key, entry.DefaultValue);
        }

        public static T Get<T> (this IConfigurationClient client, SchemaEntry<T> entry, T fallback)
        {
            return client.Get<T> (entry.Namespace, entry.Key, fallback);
        }

        public static T Get<T> (this IConfigurationClient client, string key, T fallback)
        {
            return client.Get<T> (null, key, fallback);
        }

        public static T Get<T> (this IConfigurationClient client, string namespce, string key, T fallback)
        {
            T result;
            return client.TryGet<T> (namespce, key, out result) ? result : fallback;
        }

        public static void Set<T> (this IConfigurationClient client, SchemaEntry<T> entry, T value)
        {
            client.Set (entry.Namespace, entry.Key, value);
        }

        public static void Set<T> (this IConfigurationClient client, string key, T value)
        {
            client.Set (null, key, value);
        }
    }
}
