/*************************************************************************** 
 *  AsyncHttpWebClient.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
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
using System.Net;
using System.Text;
using System.Threading;
using System.ComponentModel;

namespace Migo.Net
{
    enum DownloadType
    {
        None = 0,
        Data = 1,    
        File = 2,
        String = 3
    };

    public sealed class AsyncWebClient
    {            
        private int range = 0;
        private int timeout = (120 * 1000); // 2 minutes
        private DateTime ifModifiedSince = DateTime.MinValue;
        
        private string fileName;

        private Exception error;
        
        private Uri uri;
        private object userState;
        private DownloadType type;
        
        private IWebProxy proxy;
        private string userAgent;
        private Encoding encoding;                        
        private ICredentials credentials;

        private HttpWebRequest request;
        private HttpWebResponse response;
        
        private WebHeaderCollection headers;
        private WebHeaderCollection responseHeaders;
        
        private byte[] result;
        private Stream localFile;
        private MemoryStream memoryStream;        
        
        private TransferStatusManager tsm;
        
        private ManualResetEvent readTimeoutHandle;
        private RegisteredWaitHandle registeredTimeoutHandle;
        
        private bool busy;
        private bool completed;
        private bool cancelled;
       
        private readonly object cancelBusySync = new object ();
        
        public event EventHandler<EventArgs> ResponseReceived;        
        public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged;
        public event EventHandler<TransferRateUpdatedEventArgs> TransferRateUpdated;
            
        public event EventHandler<AsyncCompletedEventArgs> DownloadFileCompleted;        
        public event EventHandler<DownloadDataCompletedEventArgs> DownloadDataCompleted;
        public event EventHandler<DownloadStringCompletedEventArgs> DownloadStringCompleted;               
                
        public ICredentials Credentials 
		{
            get { return credentials; }
            set { credentials = value; }
        }

        public Encoding Encoding 
		{
            get {
                return encoding;
            }
            
            set {
                if (value == null) {
                    throw new ArgumentNullException ("encoding");
                } else {
                    encoding = Encoding.Default;
                }
            }
        }

        public WebHeaderCollection Headers 
		{
            get {
                return headers; 
            }
            
            set {
                if (value == null) {
                    headers = new WebHeaderCollection ();
                } else {
                    headers = value;
                }
            }
        }

        public DateTime IfModifiedSince
        {
            get { return ifModifiedSince; }
            set { ifModifiedSince = value; }
        }   

        public bool IsBusy 
        {
            get {
                lock (cancelBusySync) {
                    return busy;
                }
            }
        }
        
        public IWebProxy Proxy 
		{
            get { return proxy; }
            set { proxy = value; }
        }
        
        public int Range 
		{
            get { return range; }
            set { 
                if (range > -1) {
                    range = value;
                } else {
                    throw new ArgumentOutOfRangeException ("Range");
                }
            }
        }
        
        public HttpWebResponse Response {
            get { return response; }
        }
        
        public WebHeaderCollection ResponseHeaders {
            get { return responseHeaders; }
        }

        public AsyncWebClientStatus Status
        {            
            get {
                if (type == DownloadType.String) {
                    throw new InvalidOperationException (
                        "Status cannot be reported for string downloads"
                    );
                }
                
                lock (tsm.SyncRoot) {
                    return new AsyncWebClientStatus (
                        tsm.Progress, tsm.BytesReceived, 
                        tsm.TotalBytes, tsm.TotalBytesReceived
                    );
                }       
            }
        }
        
        public int Timeout
		{
            get {
                return timeout;
            }
            
            set {
                if (value < -1) {
                    throw new ArgumentOutOfRangeException (
                        "Value must be greater than or equal to -1"
                    );
                }
                
                timeout = value;
            }
        }        
        
        public string UserAgent 
		{
            get { return userAgent; }
            set { userAgent = value; }
        }
                
        private bool Cancelled 
		{
            get {
                lock (cancelBusySync) {
                    return cancelled; 
                }
            }
        }

        public AsyncWebClient ()
        {
            encoding = Encoding.Default;
            tsm = new TransferStatusManager ();
            tsm.ProgressChanged += OnDownloadProgressChangedHandler;
        }
            
        public void DownloadDataAsync (Uri address)
        {
            DownloadDataAsync (address, null);
        }
        
        public void DownloadDataAsync (Uri address, object userState)
        {            
            if (address == null) {
                throw new ArgumentNullException ("address");
            }            
            
            SetBusy ();
            DownloadAsync (address, DownloadType.Data, userState);
        }        
        
        public void DownloadFileAsync (Uri address, string fileName)
        {
            DownloadFileAsync (address, fileName, null, null);
        }
        
        public void DownloadFileAsync (Uri address, Stream file)
        {
            DownloadFileAsync (address, null, file, null);
        }        
        
        public void DownloadFileAsync (Uri address, string file, object userState)
        {
            DownloadFileAsync (address, file, null, userState);
        }         
        
        public void DownloadFileAsync (Uri address, Stream file, object userState)
        {
            DownloadFileAsync (address, null, file, userState);
        }         
        
        private void DownloadFileAsync (Uri address, 
                                        string filePath, 
                                        Stream fileStream, 
                                        object userState)
        {        
            if (String.IsNullOrEmpty (filePath) && 
                fileStream == null || fileStream == Stream.Null) {
                throw new ArgumentNullException ("file");
            } else if (address == null) {
                throw new ArgumentNullException ("address");
            } else if (fileStream != null) {
                if (!fileStream.CanWrite) {
                    throw new ArgumentException ("Cannot write to stream");   
                } else {
                    localFile = fileStream;
                }
            } else {
                this.fileName = filePath;
            }
              
          
            SetBusy ();                      
                            
            DownloadAsync (address, DownloadType.File, userState);
        }   
        
        public void DownloadStringAsync (Uri address)
        {
            DownloadStringAsync (address, null);
        }
         
        public void DownloadStringAsync (Uri address, object userState)
        {
            if (address == null) {
                throw new ArgumentNullException ("address");
            }
            
            SetBusy ();
            DownloadAsync (address, DownloadType.String, userState);
        }        
                
        public void CancelAsync ()
        {
            CancelAsync (true);
        }
        
        public void CancelAsync (bool deleteFile)
        {
            if (SetCancelled ())
            {  
                //Console.WriteLine ("CancelAsync Called:  {0}", this.fileName);
                AbortDownload ();
            } else {
                //Console.WriteLine ("CancelAsync not Called:  {0}", this.fileName);              
            }           
        }               

        private void AbortDownload ()
        {   
            AbortDownload (null);
        }
        
        private void AbortDownload (Exception e)
        {
            //Console.WriteLine ("AbortDownload Called:  {0}", this.fileName);
                
            error = e;
            //Console.WriteLine ("001");
                
            try {
                HttpWebRequest req = request;                
                
                if (req != null) {
                    //Console.WriteLine ("AsyncWebrequest - 002");
                        
                    req.Abort();
                    //Console.WriteLine ("AsyncWebrequest - 003");
                }
            } catch (Exception ae) {
                Console.WriteLine ("Abort Download Error:  {0}", ae.Message);
            }
        }

        private void Completed ()
        {
            Completed (null);
        }
        
        private void Completed (Exception e)
        {
            Exception err = (SetCompleted ()) ? e : error;
            
            object statePtr = userState; 
            byte[] resultPtr = result;
            bool cancelledCpy = Cancelled;

            //Console.WriteLine ("2");
            //Console.WriteLine ("CleanUp:  GetResponseCallback");                
            
            CleanUp ();
                             
            //Console.WriteLine ("2.5");
            DownloadCompleted (resultPtr, err, cancelledCpy, statePtr);
            
            Reset ();
        }

        private void CleanUp ()
        {
            //Console.WriteLine ("CleanUp");

            if (localFile != null) {
                localFile.Close ();
                localFile = null;                
            }
            
            if (memoryStream != null) {
                memoryStream.Close ();
                memoryStream = null;                
            }            
            
            if (response != null) {
                response.Close ();
                response = null;
            }
            
            result = null;
            request = null;            
            
            CleanUpHandles ();
        }
        
        private void CleanUpHandles ()
        {
            if (registeredTimeoutHandle != null) {
                registeredTimeoutHandle.Unregister (readTimeoutHandle);
                readTimeoutHandle = null;
            }            
            
            if (readTimeoutHandle != null) {
                readTimeoutHandle.Close ();
                readTimeoutHandle = null;
            }        
        }

        private bool SetBusy () 
        {
            bool ret = false;
            
            lock (cancelBusySync) {
                if (busy) {
                    throw new InvalidOperationException (
                        "Concurrent transfer operations are not supported."
                    );
                } else {
                    ret = busy = true;
                }
            }
            
            return ret;            
        }

        private bool SetCancelled ()
        {
            bool ret = false;
            
            lock (cancelBusySync) {
                if (busy && !completed && !cancelled) {
                    ret = cancelled = true;
                }
            }
            
            return ret;
        }  
        
        private bool SetCompleted ()
        {
            bool ret = false;
            
            lock (cancelBusySync) {
                if (busy && !completed && !cancelled) {
                    ret = completed = true;
                }
            }
            
            return ret;
        }         

        private void DownloadAsync (Uri uri, DownloadType type, object state)
        {            
            this.uri = uri;
            this.type = type;                        
            this.userState = state;
            
            ImplDownloadAsync ();
        }
        
        private void ImplDownloadAsync () 
        {
            try {
                tsm.Reset ();
                request = PrepRequest (uri);             

                IAsyncResult ar = request.BeginGetResponse (
                    OnResponseCallback, null
                );
                       
                ThreadPool.RegisterWaitForSingleObject (
                    ar.AsyncWaitHandle, 
                    new WaitOrTimerCallback(OnTimeout),
                    request, timeout, true
                );            
            } catch (Exception e) {
                Console.WriteLine (e.Message);            
                Console.WriteLine (e.StackTrace);
                Completed (e);
            }  
        }    
            
        private HttpWebRequest PrepRequest (Uri address)
        {
            responseHeaders = null;        
            HttpWebRequest req = HttpWebRequest.Create (address) as HttpWebRequest;
                        
            req.AllowAutoRedirect = true;
            req.Credentials = credentials;
           
            if (proxy != null) {
                req.Proxy = proxy;
            }            

			if (headers != null && headers.Count != 0) {
				
				int rangeHdr = -1;				
				string expect = headers ["Expect"];
				string contentType = headers ["Content-Type"];
				string accept = headers ["Accept"];
				string connection = headers ["Connection"];
				string userAgent = headers ["User-Agent"];
				string referer = headers ["Referer"];
				string rangeStr = headers ["Range"];
				string ifModifiedSince = headers ["If-Modified-Since"];
                
				if (!String.IsNullOrEmpty (rangeStr)) {
    				Int32.TryParse (rangeStr, out rangeHdr);
				}
				
				headers.Remove ("Expect");
				headers.Remove ("Content-Type");
				headers.Remove ("Accept");
				headers.Remove ("Connection");
				headers.Remove ("Referer");
				headers.Remove ("User-Agent");
				headers.Remove ("Range");                
				headers.Remove ("If-Modified-Since");                
                
				req.Headers = headers;

				if (!String.IsNullOrEmpty (expect)) {
					req.Expect = expect;
                }
                
				if (!String.IsNullOrEmpty (accept)) {
					req.Accept = accept;
                }

				if (!String.IsNullOrEmpty (contentType)) {
					req.ContentType = contentType;
                }

				if (!String.IsNullOrEmpty (connection)) {
					req.Connection = connection;
                }

				if (!String.IsNullOrEmpty (userAgent)) {
					req.UserAgent = userAgent;
                }

				if (!String.IsNullOrEmpty (referer)) {
					req.Referer = referer;
                }

			    if (rangeHdr > 0) {
                    req.AddRange (range);
			    }
			    
				if (!String.IsNullOrEmpty (ifModifiedSince)) {
                    DateTime modDate;
                    
                    if (DateTime.TryParse (ifModifiedSince, out modDate)) {
                        req.IfModifiedSince = modDate;
                    }
                }
			} else {
                if (!String.IsNullOrEmpty (this.userAgent)) {
                    req.UserAgent = this.userAgent;
                }

                if (this.range > 0) {
                    //Console.WriteLine ("Range Added: {0}", this.range);
                    req.AddRange (this.range);
    		    }

                if (this.ifModifiedSince > DateTime.MinValue) {
                    req.IfModifiedSince = this.ifModifiedSince;
                }
            }
            
			responseHeaders = null;                        
            
            return req;
        }         
        
        private void OnResponseCallback (IAsyncResult ar)
        {    
            Exception err = null;
            
            try {
                //Console.WriteLine ("0");
                //Console.WriteLine ("begin EndGetResponse:  {0}", this.fileName);
                
                response = request.EndGetResponse (ar) as HttpWebResponse;

                responseHeaders = response.Headers;
                OnResponseReceived (); 

                //Console.WriteLine ("end EndGetResponse");
                //Console.WriteLine ("1");      
                Download (response.GetResponseStream ());                
            } catch (ObjectDisposedException) {
            } catch (WebException we) {
                if (we.Status != WebExceptionStatus.RequestCanceled) {
                    //Console.WriteLine ("am I here?");
                    //Console.WriteLine ("Why {1}:  {0}", we.Status, this.fileName);
                    //Console.WriteLine ("Cancelled:  {0}", this.Cancelled);
                    Console.WriteLine (we.Message);
                    err = we;
                }
            } catch (Exception e) {
                //Console.WriteLine ("GetResponseCallback:  {0}", e.Message);
                err = e;                
            } finally {
                Completed (err);
                //Console.WriteLine ("3");  
            }
        }        

        // All of this download code could be abstracted
        // and put in a helper class.
        private void Download (Stream st)
        {
            long cLength = (response.ContentLength + range);

            if (cLength == 0) {
                return;
            }
            
            int nread = -1;
            int offset = 0;
            
            int length = (cLength == -1 || cLength > 8192) ? 8192 : (int) cLength;            
            
            Stream dest = null; 
            readTimeoutHandle = new ManualResetEvent (true);
            
            //Console.WriteLine ("_____________________________________________");
            //Console.WriteLine (response.Headers);
            //Console.WriteLine ("_____________________________________________\n");           

            byte[] buffer = null;
            
            bool dataDownload = false;
            bool writeToStream = false;
            
            if (type != DownloadType.String) {
                tsm.TotalBytes = cLength;
                tsm.BytesReceivedPreviously = range;
            }
            
            switch (type) {
                case DownloadType.String:
                    goto case DownloadType.Data;
                case DownloadType.Data:
                    dataDownload = true;                    
                    
                    if (cLength != -1) {
                        length = (int) cLength;                    
                        buffer = new byte[cLength];
                    } else {
                        writeToStream = true;                    
                        buffer = new byte[length];
                        dest = OpenMemoryStream ();
                    }
                    break;
                case DownloadType.File:
                    writeToStream = true;                    
                    buffer = new byte [length];
                    if (localFile == null) {
                        dest = OpenLocalFile (fileName);
                    } else {
                        dest = localFile;
                    }
                    
                    break;
            }            

            registeredTimeoutHandle = ThreadPool.RegisterWaitForSingleObject (
                readTimeoutHandle, 
                new WaitOrTimerCallback(OnTimeout),
                null, timeout, false
            );             
            
            IAsyncResult ar;
            //Console.WriteLine (writeToStream);
            
            readTimeoutHandle.Set ();            
            
            while (nread != 0)
            {        
                try {
                    readTimeoutHandle.Reset ();
                    
                    // <hack> 
                    // Yeah, Yeah, Yeah, I'll change this later, 
                    // it's here to get around abort issues.
                    
                    ar = st.BeginRead (buffer, offset, length, null, null);
                    nread = st.EndRead (ar);
                    
                    // need an auxiliary downloader class to replace this. 
                    // </hack>
                    
                    readTimeoutHandle.Set ();
                } catch { return; }
                
                if (writeToStream) {
                    dest.Write (buffer, 0, nread);
                } else {
                    offset += nread;
                    length -= nread;
                }

                if (type != DownloadType.String) {
                    tsm.AddBytes (nread);         
                } 
            }
            
            CleanUpHandles ();
            
            if (type != DownloadType.String) {            
                if (tsm.TotalBytes == -1) {
                    tsm.TotalBytes = tsm.BytesReceived;
                }
            }
            
            if (dataDownload) {
                if (writeToStream) {    
                    result = memoryStream.ToArray ();
                } else {
                    result = buffer;
                }
            }            
            
            //Console.WriteLine ("Download End");            
        }
    
        private Stream OpenLocalFile (string filePath)
        {            
            return File.Open (
                filePath, FileMode.OpenOrCreate, 
                FileAccess.Write, FileShare.None
            );            
        }
        
        private MemoryStream OpenMemoryStream ()
        {
            return memoryStream = new MemoryStream ();       
        }        
        
        private void Reset ()
        {
            lock (cancelBusySync) {
                busy = false;                
                cancelled = false;
                completed = false;
                error = null;
                fileName = String.Empty;
                ifModifiedSince = DateTime.MinValue;                
                range = 0;
                type = DownloadType.None;
                uri = null;
                userState = null;
            } 
        }
        
        private void DownloadCompleted (byte[] resultPtr, 
                                        Exception errPtr, 
                                        bool cancelledCpy, 
                                        object userStatePtr)
        {        
            switch (type) {                
            case DownloadType.Data:
                //Console.WriteLine ("DownloadCompleted Data");
                OnDownloadDataCompleted (
                    resultPtr, errPtr, cancelledCpy, userStatePtr
                );
                break;
            case DownloadType.File:
                OnDownloadFileCompleted (errPtr, cancelledCpy, userStatePtr);
                break;
            case DownloadType.String:
                string s;
                try {
                    s = Encoding.GetString (resultPtr);
                } catch {
                    s = String.Empty;
                }
            
                OnDownloadStringCompleted (
                    s, errPtr, cancelledCpy, userStatePtr
                );                        
                break;
            }            
        }

        private void OnTimeout (object state, bool timedOut) 
        {
            if (timedOut) {
                        //Console.WriteLine ("OnTimeout");
                if (SetCompleted ()) {
                    try { 
                        AbortDownload (new WebException (
                            "The operation timed out", null, 
                            WebExceptionStatus.Timeout, response
                        ));
                    } finally {
                        Completed ();
                    }
                }
            }
        }

        private void OnResponseReceived ()
        {
            EventHandler<EventArgs> handler = ResponseReceived;
            
            if (handler != null) {
                try {
                    handler (this, new EventArgs ());
                } catch {}
            }
        }
       
        private void OnDownloadProgressChanged (long bytesReceived, 
                                                long BytesToReceive,
                                                int progressPercentage, 
                                                object userState)
        {
            OnDownloadProgressChanged (
                new DownloadProgressChangedEventArgs (
                    progressPercentage, userState,                    
                    bytesReceived, BytesToReceive
                )
            );
        }

        private void OnDownloadProgressChanged (DownloadProgressChangedEventArgs args)
        {
            EventHandler <DownloadProgressChangedEventArgs> 
                handler = DownloadProgressChanged;
        
            try 
            {
                if (handler != null) {
                    handler (this, args);
                }
            } catch {}
        }

        private void OnDownloadProgressChangedHandler (object sender, 
                                                       DownloadProgressChangedEventArgs e)
        {
            OnDownloadProgressChanged (
                e.BytesReceived,
                e.TotalBytesToReceive,
                e.ProgressPercentage,
                userState
            );
        }

        private void OnDownloadDataCompleted (byte[] bytes,
                                              Exception error,
                                              bool cancelled,
                                              object userState)
        {
            OnDownloadDataCompleted (
                new DownloadDataCompletedEventArgs (
                    bytes, error, cancelled, userState
                )
            );
        }

        private void OnDownloadDataCompleted (DownloadDataCompletedEventArgs args)
        {
            EventHandler <DownloadDataCompletedEventArgs> 
                handler = DownloadDataCompleted;
                
            try
            {            
                if (handler != null) {
                    handler (this, args);
                }
            } catch {}
        }        
        
        private void OnDownloadFileCompleted (Exception error,
                                              bool cancelled,
                                              object userState)
        {
            OnDownloadFileCompleted (
                new AsyncCompletedEventArgs (error, cancelled, userState)
            );
        }

        private void OnDownloadFileCompleted (
            AsyncCompletedEventArgs args)
        {
            EventHandler <AsyncCompletedEventArgs> 
                handler = DownloadFileCompleted;

            try
            {            
                if (handler != null) {
                    handler (this, args);
                }
            } catch {}
        }
        
        private void OnDownloadStringCompleted (string resultStr,
                                                Exception error,
                                                bool cancelled,
                                                object userState)
        {
            OnDownloadStringCompleted (
                new DownloadStringCompletedEventArgs (
                    resultStr, error, cancelled, userState
                )
            );
        }

        private void OnDownloadStringCompleted (DownloadStringCompletedEventArgs args)
        {
            EventHandler <DownloadStringCompletedEventArgs> 
                handler = DownloadStringCompleted;
                
            try
            {            
                if (handler != null) {
                    handler (this, args);
                }
            } catch {}
        }
    }
}