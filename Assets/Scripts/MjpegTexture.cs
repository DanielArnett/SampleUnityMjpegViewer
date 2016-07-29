using UnityEngine;
using System;

/// <summary>
/// A Unity3D Script to dipsplay Mjpeg streams. Apply this script to the mesh that you want to use to view the Mjpeg stream. 
/// </summary>
public class MjpegTexture : MonoBehaviour
{
	/// <param name="streamAddress">
	/// Set this to be the network address of the mjpg stream. 
	/// Example: "http://extcam-16.se.axis.com/mjpg/video.mjpg"
	/// </param>
	public string streamAddress;
    Texture2D tex;
    byte[] pixelMap;
    const int numOfCols = 16;
    const int numOfRows = numOfCols / 2;
	const int numOfPixels = numOfCols * numOfRows;
    // Flag showing when to update the frame
    bool updateFrame = false;

    MjpegProcessor mjpeg;
    //System.Diagnostics.Stopwatch watch;
    int frameCount = 0;
    public void Start()
    {
		mjpeg = new MjpegProcessor();
		mjpeg.FrameReady += mjpeg_FrameReady;
		mjpeg.Error += mjpeg_Error;
        Uri mjpeg_address = new Uri(streamAddress);
		mjpeg.ParseStream(mjpeg_address);
        // Create a 16x16 texture with PVRTC RGBA4 format
        // and will it with raw PVRTC bytes.
        tex = new Texture2D(800, 600, TextureFormat.PVRTC_RGBA4, false);
    }
    private void mjpeg_FrameReady(object sender, FrameReadyEventArgs e)
    {
        updateFrame = true;
    }
    void mjpeg_Error(object sender, ErrorEventArgs e)
    {
        Debug.Log("Error received while reading the MJPEG.");
    }

    // Update is called once per frame
    void Update()
    {
        if (updateFrame)
        {
			tex.LoadImage(mjpeg.CurrentFrame);
            tex.Apply();
            // Assign texture to renderer's material.
            GetComponent<Renderer>().material.mainTexture = tex;
            updateFrame = false;
        }
    }

    void OnDestroy()
    {
		mjpeg.StopStream();
    }
}
