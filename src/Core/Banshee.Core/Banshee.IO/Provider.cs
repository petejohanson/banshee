//
// Provider.cs
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

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Mono.Addins;

using Hyena;
using Banshee.Base;
using Banshee.Configuration;

namespace Banshee.IO
{
    public static class Provider
    {
        private static IProvider provider;
        private static IDirectory directory;
        private static IFile file;

        public static IEnumerable<TypeExtensionNode> GetOrderedExtensions (string extensionPoint, params string [] ordered_ids)
        {
                return AddinManager.GetExtensionNodes (extensionPoint)
                                   .Cast<TypeExtensionNode> ()
                                   .Where (n => n.HasId)
                                   .OrderBy (n => {
                                       var o = Array.IndexOf (ordered_ids, n.Id);
                                       return o == -1 ? int.MaxValue : o;
                                   });
        }

        static Provider () {
            lock (typeof (Provider)) {
                if (provider != null) {
                    return;
                }

                var extensions = GetOrderedExtensions (
                    "/Banshee/Platform/IOProvider",
                    ProviderSchema.Get (),
                    "Banshee.IO.Gio.Provider", "Banshee.IO.Unix.Provider", "Banshee.IO.SystemIO.Provider"
                );

                foreach (var node in extensions) {
                    try {
                        provider = (IProvider)node.CreateInstance (typeof (IProvider));
                        break;
                    } catch (Exception e) {
                        Log.Warning ("IO provider extension failed to load", e.Message);
                    }
                }

                if (provider == null) {
                    provider = new Banshee.IO.SystemIO.Provider ();
                }

                Log.DebugFormat ("IO provider extension loaded ({0})", provider.GetType ().FullName);

                directory = (IDirectory)Activator.CreateInstance (provider.DirectoryProvider);
                file = (IFile)Activator.CreateInstance (provider.FileProvider);
            }
        }

        public static void SetProvider (IProvider customProvider)
        {
            provider = customProvider;
            directory = (IDirectory)Activator.CreateInstance (provider.DirectoryProvider);
            file = (IFile)Activator.CreateInstance (provider.FileProvider);
        }

        internal static IDirectory Directory {
            get { return directory; }
        }

        internal static IFile File {
            get { return file; }
        }

        public static bool LocalOnly {
            get { return provider == null ? true : provider.LocalOnly; }
        }

        internal static IDemuxVfs CreateDemuxVfs (string file)
        {
            return (IDemuxVfs)Activator.CreateInstance (provider.DemuxVfsProvider, new object [] { GetPath (file) });
        }

        internal static string GetPath (string uri)
        {
            if (LocalOnly && !String.IsNullOrEmpty (uri) && uri[0] != '/' && uri.StartsWith ("file://")) {
                return new SafeUri (uri).LocalPath;
            }

            return uri;
        }

        internal static readonly SchemaEntry<string> ProviderSchema = new SchemaEntry<string> (
            "core", "io_provider",
            "Banshee.IO.Gio.Provider",
            "Set the IO provider backend in Banshee",
            "Can be either \"Banshee.IO.SystemIO.Provider\" (.NET System.IO), " +
                "\"Banshee.IO.Gio.Provider\" (GIO), or " +
                "\"Banshee.IO.Unix.Provider\" (Native Unix/POSIX), or " +
                "takes effect on Banshee start (restart necessary)"
        );
    }
}
