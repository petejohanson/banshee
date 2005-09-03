/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  RipTransaction.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using Mono.Unix;

namespace Banshee
{
    public delegate void CdRipProgressCallback(IntPtr ripper, int seconds,
        IntPtr user_info);
        
    public delegate void AudioCdRipperProgressHandler(object o, 
        AudioCdRipperProgressArgs args);
        
    public class AudioCdRipperProgressArgs : EventArgs
    {
        public int SecondsEncoded;
        public int TotalSeconds;
        public AudioCdTrackInfo Track;
    }

    public class AudioCdRipper : IDisposable
    {
        private HandleRef handle;
        private CdRipProgressCallback progressCallback;
        private AudioCdTrackInfo currentTrack;
        
        public event AudioCdRipperProgressHandler Progress;

        public AudioCdRipper(string device, int paranoia_mode, 
            string encoder_pipeline)
        {
            IntPtr ptr = cd_rip_new(device, paranoia_mode, encoder_pipeline);
            if(ptr == IntPtr.Zero)
                throw new ApplicationException(Catalog.GetString(
                    "Could not create CD Ripper"));
            
            handle = new HandleRef(this, ptr);
            
            progressCallback = new CdRipProgressCallback(OnProgress);
            cd_rip_set_progress_callback(handle, progressCallback);
        }
        
        public void Dispose()
        {
            cd_rip_free(handle);
        }
        
        public bool RipTrack(AudioCdTrackInfo track, int track_number, 
            string outputUri)
        {
            currentTrack = track;
            bool result = cd_rip_rip_track(handle, outputUri, track_number, 
                track.Artist, track.Album, track.Title, track.Genre, 
                (int)track.TrackNumber, (int)track.TrackCount, IntPtr.Zero);
            track = null;
            return result;
        }
        
        public void Cancel()
        {
            cd_rip_cancel(handle);
        }
        
        public string Error
        {
            get {
                IntPtr errPtr = cd_rip_get_error(handle);
                if(errPtr == IntPtr.Zero)
                    return null;
                
                return GLib.Marshaller.Utf8PtrToString(errPtr);
            }
        }
        
        private void OnProgress(IntPtr ripper, int seconds, IntPtr user_info)
        {
            AudioCdRipperProgressHandler handler = Progress;
            if(handler == null)
                return;
                
            AudioCdRipperProgressArgs args = new AudioCdRipperProgressArgs();
            args.TotalSeconds = (int)currentTrack.Duration;
            args.SecondsEncoded = seconds;
            args.Track = currentTrack;
            
            handler(this, args); 
        }
        
        [DllImport("libbanshee")]
        private static extern IntPtr cd_rip_new(string device,
            int paranoia_mode, string encoder_pipeline);
            
        [DllImport("libbanshee")]
        private static extern void cd_rip_free(HandleRef ripper);
        
        [DllImport("libbanshee")]
        private static extern bool cd_rip_rip_track(HandleRef ripper, 
            string uri, int track_number, string md_artist, string md_album, 
            string md_title, string md_genre, int md_track_number, 
            int md_track_count, IntPtr user_info);
            
        [DllImport("libbanshee")]
        private static extern void cd_rip_set_progress_callback(
            HandleRef ripper, CdRipProgressCallback cb);
            
        [DllImport("libbanshee")]
        private static extern void cd_rip_cancel(HandleRef ripper);
        
        [DllImport("libbanshee")]
        private static extern IntPtr cd_rip_get_error(HandleRef ripper);
    }

    public class RipTransaction : LibraryTransaction
    {
        private ArrayList tracks = new ArrayList();
        private AudioCdRipper ripper;
        private string device;
        private int currentIndex = 0;
        private int overallProgress = 0;
        private string status;
        
        // speed calculations
        private int currentSeconds = 0;
        private int lastPollSeconds = 0;
        private uint pollDelay = 1000;

        public event HaveTrackInfoHandler HaveTrackInfo;

        public override string Name {
            get {
                return "CD Ripping Transaction";
            }
        }
        
        public RipTransaction()
        {
            showCount = false;
        }

        public void QueueTrack(AudioCdTrackInfo track)
        {
            if(device == null) {
                device = track.Device;
            } else if(device != track.Device) {
                throw new ApplicationException(String.Format(Catalog.GetString(
                    "The device node '{0}' differs from the device node " + 
                    "already set for previously queued tracks ({1})"),
                    track.Device, device));
            }
            
            tracks.Add(track);
            totalCount += track.Duration;
        }
        
        public override void Run()
        {
            int current = 1;
            statusMessage = Catalog.GetString("Initializing CD Drive");
        
            PipelineProfile profile = 
                PipelineProfile.GetConfiguredProfile("Ripping");
            
            string encodePipeline;
            
            try {
                encodePipeline = profile.Pipeline;
            } catch(PipelineProfileException e) {
                DebugLog.Add("Cannot Rip CD: " + e.Message);
                return;
            }
        
            DebugLog.Add("Ripping CD and Encoding with Pipeline: " 
                + encodePipeline);
        
            ripper = new AudioCdRipper(device, 0, encodePipeline);
            ripper.Progress += OnRipperProgress;
            
            uint timeoutId = GLib.Timeout.Add(pollDelay, OnTimeout);
            
            foreach(AudioCdTrackInfo track in tracks) {
                if(cancelRequested)
                    break;
         
                status = String.Format(Catalog.GetString(
                    "Ripping {0} of {1} : {2} - {3}"), current++, QueueSize,
                    track.Artist, track.Title);
                statusMessage = status;
                    
                string filename = "file://" + 
                    FileNamePattern.BuildFull(track, profile.Extension);
                    
                if(!ripper.RipTrack(track, track.TrackIndex + 1, filename)) {
                    break;
                }
                
                overallProgress += (int)track.Duration;
                
                if(!cancelRequested) {
                    TrackInfo lti = new LibraryTrackInfo(filename, track);
                    
                    HaveTrackInfoHandler handler = HaveTrackInfo;
                    if(handler != null) {
                        HaveTrackInfoArgs args = new HaveTrackInfoArgs();
                        args.TrackInfo = lti;
                        handler(this, args);
                    }
                }
                
                currentIndex++;
            }
                   
            if(timeoutId > 0)
                GLib.Source.Remove(timeoutId);
                   
            ripper.Dispose();
        }
        
        private bool OnTimeout()
        {
            int diff = currentSeconds - lastPollSeconds;  
            lastPollSeconds = currentSeconds;
            
            if(diff <= 0) {
                statusMessage = status;
                return true;
            }
            
            statusMessage = status + String.Format(" ({0}x)", diff);
            return true;
        }
        
        private void OnRipperProgress(object o, AudioCdRipperProgressArgs args)
        {
            if(cancelRequested && ripper != null)
                ripper.Cancel();
                
            if(args.SecondsEncoded == 0)
                return;
                
            currentCount = args.SecondsEncoded + overallProgress;
            currentSeconds = args.SecondsEncoded;
        }
        
        public int QueueSize
        {
            get {
                return tracks.Count;
            }
        }
    }
}
