using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Gahwit.Boot
{
    // The phone's own share sheet, for postcards (missing/20): Snapchat, Instagram, WhatsApp, or saving the image.
    public static class ShareSheet
    {
        // Returns false when sharing isn't possible here; the image is still saved at path.
        public static bool Share(string path, string text)
        {
#if UNITY_IOS && !UNITY_EDITOR
            GahwitShare(path, text);
            return true;
#elif UNITY_ANDROID && !UNITY_EDITOR
            return ShareOnAndroid(path, text);
#else
            Debug.Log($"[Share] Postcard saved at {path}: {text}");
            return false;
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void GahwitShare(string path, string text);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        // The image goes into the phone's pictures through MediaStore (no permission needed from Android 10), and the
        // share intent points at it.
        static bool ShareOnAndroid(string path, string text)
        {
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var resolver = activity.Call<AndroidJavaObject>("getContentResolver");
                using var values = new AndroidJavaObject("android.content.ContentValues");
                values.Call("put", "_display_name", Path.GetFileName(path));
                values.Call("put", "mime_type", "image/png");
                using var media = new AndroidJavaClass("android.provider.MediaStore$Images$Media");
                using var collection = media.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI");
                using var uri = resolver.Call<AndroidJavaObject>("insert", collection, values);
                using (var stream = resolver.Call<AndroidJavaObject>("openOutputStream", uri))
                {
                    stream.Call("write", File.ReadAllBytes(path));
                    stream.Call("close");
                }
                using var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND");
                intent.Call<AndroidJavaObject>("setType", "image/png");
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", uri);
                if (!string.IsNullOrEmpty(text)) intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", text);
                intent.Call<AndroidJavaObject>("addFlags", 1); // FLAG_GRANT_READ_URI_PERMISSION
                using var intentClass = new AndroidJavaClass("android.content.Intent");
                using var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "");
                activity.Call("startActivity", chooser);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Share] " + e.Message);
                return false;
            }
        }
#endif
    }
}
