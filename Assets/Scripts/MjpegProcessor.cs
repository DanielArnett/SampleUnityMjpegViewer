using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
// If you see the error "The type or namespace name `Drawing' does not exist in the namespace `System'. Are you missing an assembly reference?"
// Then you need to copy "System.Drawing.dll" from the Mono directory into your Unity Project Directory
using System.Drawing;

public class MjpegProcessor {
    public Bitmap bitmap { get; set; }
    // 2 byte header for JPEG images
    private readonly byte[] JpegHeader = new byte[] { 0xff, 0xd8 };
    // pull down 1024 bytes at a time
    private const int ChunkSize = 1024*50;
    // used to cancel reading the stream
    private bool _streamActive;
    // current encoded JPEG image
    public byte[] CurrentFrame { get; private set; }
    // WPF, Silverlight
    //public BitmapImage BitmapImage { get; set; }
    // used to marshal back to UI thread
    private SynchronizationContext _context;
    public byte[] latestFrame = null;
    private bool responseReceived = false;

    // event to get the buffer above handed to you
    public event EventHandler<FrameReadyEventArgs> FrameReady;
    public event EventHandler<ErrorEventArgs> Error;

    public MjpegProcessor()
    {
        _context = SynchronizationContext.Current;
    }


    public void ParseStream(Uri uri)
    {
        ParseStream(uri, null, null);
    }

    public void ParseStream(Uri uri, string username, string password)
    {
        print("Parsing Stream " + uri.ToString());
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
        if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
            request.Credentials = new NetworkCredential(username, password);
        // asynchronously get a response
        request.BeginGetResponse(OnGetResponse, request);
    }

    public void StopStream()
    {
        _streamActive = false;
    }
    public static int FindBytes(byte[] buff, byte[] search)
    {
        // enumerate the buffer but don't overstep the bounds
        for (int start = 0; start < buff.Length - search.Length; start++)
        {
            // we found the first character
            if (buff[start] == search[0])
            {
                int next;

                // traverse the rest of the bytes
                for (next = 1; next < search.Length; next++)
                {
                    // if we don't match, bail
                    if (buff[start + next] != search[next])
                        break;
                }

                if (next == search.Length)
                    return start;
            }
        }
        // not found
        return -1;
    }
    public static int FindBytesInReverse(byte[] buff, byte[] search)
    {
        // enumerate the buffer but don't overstep the bounds
        for (int start = buff.Length - search.Length - 1; start > 0; start--)
        {
            // we found the first character
            if (buff[start] == search[0])
            {
                int next;

                // traverse the rest of the bytes
                for (next = 1; next < search.Length; next++)
                {
                    // if we don't match, bail
                    if (buff[start + next] != search[next])
                        break;
                }

                if (next == search.Length)
                    return start;
            }
        }
        // not found
        return -1;
    }

    private void print(String str) 
    {
        Debug.Log(str);
    }
    private void OnGetResponse(IAsyncResult asyncResult)
    {
        responseReceived = true;
        print("OnGetResponse");
        byte[] imageBuffer = new byte[1024 * 1024];

        print("Starting request");
        // get the response
        HttpWebRequest req = (HttpWebRequest)asyncResult.AsyncState;

        try
        {
            print("OnGetResponse try entered.");
            HttpWebResponse resp = (HttpWebResponse)req.EndGetResponse(asyncResult);
            print("response received");
            // find our magic boundary value
            string contentType = resp.Headers["Content-Type"];
            if (!string.IsNullOrEmpty(contentType) && !contentType.Contains("="))
            {
                print("MJPEG Exception thrown");
                throw new Exception("Invalid content-type header.  The camera is likely not returning a proper MJPEG stream.");
            }

            string boundary = resp.Headers["Content-Type"].Split('=')[1].Replace("\"", "");
            byte[] boundaryBytes = Encoding.UTF8.GetBytes(boundary.StartsWith("--") ? boundary : "--" + boundary);

            Stream s = resp.GetResponseStream();
            BinaryReader br = new BinaryReader(s);

            _streamActive = true;
            byte[] buff = br.ReadBytes(ChunkSize);

            while (_streamActive)
            {
                // find the JPEG header
                int imageStart = FindBytes(buff, JpegHeader);// buff.Find(JpegHeader);

                if (imageStart != -1)
                {
                    // copy the start of the JPEG image to the imageBuffer
                    int size = buff.Length - imageStart;
                    Array.Copy(buff, imageStart, imageBuffer, 0, size);

                    while (true)
                    {
                        buff = br.ReadBytes(ChunkSize);

                        // Find the end of the jpeg
                        int imageEnd = FindBytes(buff, boundaryBytes);
                        if (imageEnd != -1)
                        {
                            // copy the remainder of the JPEG to the imageBuffer
                            Array.Copy(buff, 0, imageBuffer, size, imageEnd);
                            size += imageEnd;

                            // Copy the latest frame into `CurrentFrame`
                            byte[] frame = new byte[size];
                            Array.Copy(imageBuffer, 0, frame, 0, size);
                            CurrentFrame = frame;

                            // tell whoever's listening that we have a frame to draw
                            if (FrameReady != null)
                                FrameReady(this, new FrameReadyEventArgs());
                            // copy the leftover data to the start
                            Array.Copy(buff, imageEnd, buff, 0, buff.Length - imageEnd);

                            // fill the remainder of the buffer with new data and start over
                            byte[] temp = br.ReadBytes(imageEnd);

                            Array.Copy(temp, 0, buff, buff.Length - imageEnd, temp.Length);
                            break;
                        }

                        // copy all of the data to the imageBuffer
                        Array.Copy(buff, 0, imageBuffer, size, buff.Length);
                        size += buff.Length;

                        if (!_streamActive)
                        {
                            print("CLOSING");
                            resp.Close();
                            break;
                        }
                    }
                }
            }
            resp.Close();
        }
        catch (Exception ex)
        {
            if (Error != null)
                _context.Post(delegate { Error(this, new ErrorEventArgs() { Message = ex.Message }); }, null);

            return;
        }
    }
}

public class FrameReadyEventArgs : EventArgs
{
	
}

public sealed class ErrorEventArgs : EventArgs

{
    public string Message { get; set; }
    public int ErrorCode { get; set; }
}