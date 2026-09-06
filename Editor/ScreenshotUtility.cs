using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NoFowl.Editor
{
    [ExecuteInEditMode]
    public class ScreenshotUtility
    {
        [MenuItem("Fowl/Screenshot")]
        static void Screenshot()
        {
            ScreenCapture.CaptureScreenshot("Assets/Screenshot_" + Application.productName + System.DateTime.Now.ToString("dd-MM-yy_HH-mm") + ".png");
        }
        [MenuItem("Fowl/Hi-Res Screenshot")]
        static void HDScreenshot()
        {
            ScreenCapture.CaptureScreenshot("Assets/Screenshot_" + Application.productName + System.DateTime.Now.ToString("dd-MM-yy_HH-mm") + ".png", 2);
        }
    }
}
