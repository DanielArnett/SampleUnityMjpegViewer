using UnityEngine;
using System;

using UnityEngine.UI;

/// <summary>
/// A Unity3D Script to dipsplay Mjpeg streams. Apply this script to the mesh that you want to use to view the Mjpeg stream. 
/// </summary>
public class MjpegTexture : MonoBehaviour
{
    /// <param name="streamAddress">
    /// Set this to be the network address of the mjpg stream. 
    /// Example: "http://extcam-16.se.axis.com/mjpg/video.mjpg"
    /// </param>
    [Tooltip("Set this to be the network address of the mjpg stream. ")]
    public string streamAddress;

    /// <summary>
    /// Show fps (OnGUI).
    /// </summary>
    [Tooltip("Show fps (OnGUI).")]
    public bool showFps = true;

    /// <summary>
    /// Chunk size for stream processor in kilobytes.
    /// </summary>
    [Tooltip("Chunk size for stream processor in kilobytes.")]
    public int chunkSize = 4;

    Texture2D tex;

    const int initWidth = 2;
    const int initHeight = 2;

    bool updateFrame = false;

    MjpegProcessor mjpeg;

    float deltaTime = 0.0f;
    float mjpegDeltaTime = 0.0f;





    public void Start()
    {
        mjpeg = new MjpegProcessor(chunkSize * 1024);
        mjpeg.FrameReady += OnMjpegFrameReady;
        mjpeg.Error += OnMjpegError;
        Uri mjpegAddress = new Uri(streamAddress);
        mjpeg.ParseStream(mjpegAddress);
        // Create a 16x16 texture with PVRTC RGBA4 format
        // and will it with raw PVRTC bytes.
        tex = new Texture2D(initWidth, initHeight, TextureFormat.PVRTC_RGBA4, false);
    }
    private void OnMjpegFrameReady(object sender, FrameReadyEventArgs e)
    {
        updateFrame = true;
    }
    void OnMjpegError(object sender, ErrorEventArgs e)
    {
        Debug.Log("Error received while reading the MJPEG.");
    }
    
    // Update is called once per frame
    void Update()
    {
        deltaTime += Time.deltaTime;

        if (updateFrame)
        {
            tex.LoadImage(mjpeg.CurrentFrame);
            // tex.Apply();
            // Assign texture to renderer's material.
            GetComponent<Renderer>().material.mainTexture = tex;
            updateFrame = false;

            mjpegDeltaTime += (deltaTime - mjpegDeltaTime) * 0.2f;

            deltaTime = 0.0f;
        }
    }

    void DrawFps()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(20, 20 + (h * 4 / 100 + 10), w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 4 / 100;
        style.normal.textColor = new Color(255, 255, 255, 255);
        float msec = mjpegDeltaTime * 1000.0f;
        float fps = 1.0f / mjpegDeltaTime;
        string text = string.Format("MJPEG: {0:0.0} ms ({1:0.} fps)", msec, fps);
        GUI.Label(rect, text, style);
    }

    void OnGUI()
    {
        if (showFps) DrawFps();
    }

    void OnDestroy()
    {
        mjpeg.StopStream();
    }
}