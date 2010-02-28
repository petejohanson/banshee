//
// FileNamePattern.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2010 Novell, Inc.
// Copyright (C) 2009 Alexander Kojevnikov
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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using Mono.Unix;

using Banshee.Configuration;
using Banshee.Configuration.Schema;
using Banshee.Collection;

namespace Banshee.Base
{
    public static class FileNamePattern
    {
        public delegate string ExpandTokenHandler (TrackInfo track, object replace);
        public delegate string FilterHandler (string path);

        public static FilterHandler Filter;
        public static PathPattern MusicPattern { get; set; }

        public static IEnumerable<Conversion> PatternConversions { get { return MusicPattern.PatternConversions; } }
        public static string DefaultFolder { get { return MusicPattern.DefaultFolder; } }
        public static string DefaultFile { get { return MusicPattern.DefaultFile; } }
        public static string DefaultPattern { get { return MusicPattern.DefaultPattern; } }
        public static string [] SuggestedFolders { get { return MusicPattern.SuggestedFolders; } }
        public static string [] SuggestedFiles { get { return MusicPattern.SuggestedFiles; } }

        public static void AddConversion (string token, string name, ExpandTokenHandler handler)
        {
            MusicPattern.AddConversion (token, name, handler);
        }

        public static string CreateFolderFilePattern (string folder, string file)
        {
            return MusicPattern.CreateFolderFilePattern (folder, file);
        }

        public static string CreatePatternDescription (string pattern)
        {
            return MusicPattern.CreatePatternDescription (pattern);
        }

        public static string CreateFromTrackInfo (TrackInfo track)
        {
            return MusicPattern.CreateFromTrackInfo (track);
        }

        public static string CreateFromTrackInfo (string pattern, TrackInfo track)
        {
            return MusicPattern.CreateFromTrackInfo (pattern, track);
        }

        public static string Convert (string pattern, Func<Conversion, string> handler)
        {
            return MusicPattern.Convert (pattern, handler);
        }

        public static string BuildFull (string base_dir, TrackInfo track)
        {
            return MusicPattern.BuildFull (base_dir, track);
        }

        public static string BuildFull (string base_dir, TrackInfo track, string ext)
        {
            return MusicPattern.BuildFull (base_dir, track, ext);
        }

        public struct Conversion
        {
            private readonly string token;
            private readonly string name;
            private readonly ExpandTokenHandler handler;

            private readonly string token_string;

            public Conversion (string token, string name, ExpandTokenHandler handler)
            {
                this.token = token;
                this.name = name;
                this.handler = handler;

                this.token_string = "%" + this.token + "%";
            }

            public string Token {
                get { return token; }
            }

            public string Name {
                get { return name; }
            }

            public ExpandTokenHandler Handler {
                get { return handler; }
            }

            public string TokenString {
                get { return token_string; }
            }
        }

        public static string Escape (string input)
        {
            return Hyena.StringUtil.EscapeFilename (input);
        }
    }
}
