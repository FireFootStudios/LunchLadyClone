using System;
using System.IO;
using UnityEngine;

public class ScreenshotManager : SingletonBase<ScreenshotManager>
{
    private const string folderPath = "Screenshots";

    public void TakeScreenshot(int superSize = 1)
    {
        // Ensure the folder exists
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Debug.Log($"Created folder: {folderPath}");
        }

        // Format the current date and time for the filename
        string dateTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss" + Time.frameCount);
        string path = $"Screenshots/screenshot_{dateTime}.png";

        // Take a screenshot with the specified resolution multiplier
        ScreenCapture.CaptureScreenshot(path, superSize);
        Debug.Log($"Screenshot saved to {path}");
    }

    //private void Update()
    //{
    //    // Press 'P' to take a screenshot
    //    if (Input.GetKeyDown(KeyCode.P))
    //    {
    //        CaptureScreenshot(2); // Using a superSize of 2 for higher resolution
    //    }
    //}
}