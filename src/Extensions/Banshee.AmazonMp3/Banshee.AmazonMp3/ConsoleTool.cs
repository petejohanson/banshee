// 
// ConsoleTool.cs
//  
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2010 Novell, Inc.
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
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Hyena.Downloader;

namespace Banshee.AmazonMp3
{
    public static class ConsoleTool
    {
        private static void Usage ()
        {
            Console.Error.WriteLine ("Usage: bamz [-o output_dir] amz_files...");
            Console.Error.WriteLine ();
            Console.Error.WriteLine ("Written by Aaron Bockover <aaron@abock.org>");
            Console.Error.WriteLine ("Copyright 2010 Novell, Inc.");
            Environment.Exit (1);
        }

        public static void Main (string [] args)
        {
            var output_path = Environment.CurrentDirectory;

            if (args.Length < 1) {
                Usage ();
                return;
            }

            var amz_files = new Queue<AmzXspfPlaylist> ();

            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-o") {
                    if (i < args.Length - 1) {
                        output_path = args[++i];
                    } else {
                        Usage ();
                        return;
                    }
                } else if (args[i] == "--help" || args[i] == "-h") {
                    Usage ();
                    return;
                } else {
                    try {
                        amz_files.Enqueue (new AmzXspfPlaylist (args[i]));
                    } catch {
                        Console.Error.WriteLine ("Invalid .amz file: {0}", args[i]);
                    }
                }
            }

            while (amz_files.Count > 0) {
                int i = 1;
                var playlist = amz_files.Dequeue ();

                var download_manager = new DownloadManager { MaxConcurrentDownloaders = 1 };

                download_manager.Finished += d => {
                    if (!d.State.Success) {
                        Console.WriteLine ("[{0}/{1}] {2}: {3}", i, playlist.DownloadableTrackCount,
                            d.Name, d.State.FailureException.Message);
                    } else {
                        Console.WriteLine ();
                    }
                    i++;
                };

                download_manager.Progress += d => {
                    int progress_bar_size = Console.WindowWidth - 65;
                    Console.Write ("\r{0} {1} |{2}| {3} {4}",
                        Field (String.Format ("[{0}/{1}]", i, playlist.DownloadableTrackCount), 7),
                        Field (d.Name, 35),
                        Field (String.Empty.PadRight ((int)Math.Ceiling (progress_bar_size
                            * d.State.PercentComplete), '='), progress_bar_size),
                        Field (((int)Math.Round (d.State.PercentComplete * 100)).ToString () + "%", 4),
                        Field (String.Format ("{0:0.00} KB/s", d.State.TransferRate / 1024), 12));
                };

                Console.WriteLine ("Downloading Album");

                foreach (var track in playlist.DownloadableTracks) {
                    download_manager.QueueDownloader (new AmzMp3Downloader (track) {
                        OutputPath = output_path
                    });
                }

                download_manager.WaitUntilFinished ();
            }
        }

        private static string Field (string s, int l)
        {
            if (s.Length > l) {
                s = s.Substring (0, l - 3).TrimEnd () + "...";
            }
            return s.PadRight (l);
        }
    }
}
