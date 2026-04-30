using System;
using UnityEngine;

/// Opens the Android system file picker filtered to glTF/glb files and returns
/// the selected file's local filesystem path via a callback.
///
/// Usage:
///     AndroidFilePicker.OpenFilePicker(path => {
///         if (!string.IsNullOrEmpty(path))
///             RuntimeModelLoader.Instance.LoadFromPath(path);
///     });
///
/// In the Unity Editor or on non-Android platforms it invokes the callback with
/// an empty string so callers can handle the no-op gracefully.
public static class AndroidFilePicker
{
    // Unity game-object name that Android will send the result to via UnitySendMessage.
    private const string ReceiverObjectName = "AndroidFilePickerReceiver";

    private static Action<string> _pendingCallback;

    /// Launches the Android system file picker. The callback is invoked on the main thread with the resolved file path,
    /// or an empty string if the user cancelled or an error occurred.
    public static void OpenFilePicker(Action<string> onPathResolved)
    {
        _pendingCallback = onPathResolved;

#if UNITY_ANDROID && !UNITY_EDITOR
        EnsureReceiverExists();

        try
        {
            // Build the Intent: ACTION_OPEN_DOCUMENT lets the user pick any
            // single file while keeping URI access grants alive.
            using var intentClass   = new AndroidJavaClass("android.content.Intent");
            using var intent        = new AndroidJavaObject("android.content.Intent");

            string actionOpenDoc = intentClass.GetStatic<string>("ACTION_OPEN_DOCUMENT");
            intent.Call<AndroidJavaObject>("setAction", actionOpenDoc);

            // Accept both glb (binary glTF) and gltf (JSON glTF).
            // Android's MIME type for both is broadly "model/*" but we also
            // accept "*/*" as a fallback and filter by extension on the way back.
            intent.Call<AndroidJavaObject>("setType", "*/*");
            intent.Call<AndroidJavaObject>("addCategory",
                intentClass.GetStatic<string>("CATEGORY_OPENABLE"));

            // Limit the picker to glTF/glb via extra MIME types where supported.
            string[] mimeTypes = { "model/gltf+json", "model/gltf-binary", "application/octet-stream" };
            using var arrClass  = new AndroidJavaClass("java.lang.String");
            var mimeArray       = AndroidJNIHelper.ConvertToJNIArray(mimeTypes);
            var extraMimeKey    = intentClass.GetStatic<string>("EXTRA_MIME_TYPES");
            intent.Call<AndroidJavaObject>("putExtra", extraMimeKey, mimeArray);

            // Start the picker via the UnityPlayer activity.
            using var unityPlayer   = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity      = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Request code 7741 is arbitrary, just needs to be consistent.
            activity.Call("startActivityForResult", intent, 7741);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AndroidFilePicker] Failed to open file picker: {e}");
            _pendingCallback?.Invoke(string.Empty);
            _pendingCallback = null;
        }
#else
        Debug.Log("[AndroidFilePicker] File picker is only available on Android device builds. Invoking callback with empty path.");
        onPathResolved?.Invoke(string.Empty);
        _pendingCallback = null;
#endif
    }


    // Called by the receiver MonoBehaviour when Android returns a result.  
    internal static void OnPickerResult(string contentUriString)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (string.IsNullOrEmpty(contentUriString))
        {
            Debug.Log("[AndroidFilePicker] Picker returned empty URI (user cancelled).");
            _pendingCallback?.Invoke(string.Empty);
            _pendingCallback = null;
            return;
        }

        string resolvedPath = ResolveContentUri(contentUriString);
        Debug.Log($"[AndroidFilePicker] Resolved path: '{resolvedPath}'");

        _pendingCallback?.Invoke(resolvedPath);
        _pendingCallback = null;
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// Converts a content:// URI to a usable file path.
    /// For files in external storage this extracts the data column.
    /// For files in the app's own storage (content URIs from FileProvider) it
    /// copies the file to the app cache and returns that path.
    private static string ResolveContentUri(string uriString)
    {
        try
        {
            using var unityPlayer   = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity      = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var contentResolver = activity.Call<AndroidJavaObject>("getContentResolver");

            using var uri           = new AndroidJavaClass("android.net.Uri")
                                        .CallStatic<AndroidJavaObject>("parse", uriString);

            // Try MediaStore / Downloads column first (works for most external files).
            string path = TryResolveViaMediaStore(contentResolver, uri);
            if (!string.IsNullOrEmpty(path))
                return path;

            // Fall back: copy the stream into app-private cache and use that path.
            return CopyToCache(activity, contentResolver, uri);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AndroidFilePicker] Failed to resolve URI '{uriString}': {e}");
            return string.Empty;
        }
    }

    private static string TryResolveViaMediaStore(AndroidJavaObject contentResolver, AndroidJavaObject uri)
    {
        try
        {
            // Query the _data column — present for MediaStore and Downloads URIs.
            string[] projection = { "_data" };
            using var cursor = contentResolver.Call<AndroidJavaObject>(
                "query", uri, projection, null, null, null);

            if (cursor == null || !cursor.Call<bool>("moveToFirst"))
                return string.Empty;

            int colIndex = cursor.Call<int>("getColumnIndexOrThrow", "_data");
            string path  = cursor.Call<string>("getString", colIndex);
            cursor.Call("close");
            return path;
        }
        catch
        {
            // _data column doesn't exist (scoped storage / SAF) — fall through.
            return string.Empty;
        }
    }

    private static string CopyToCache(AndroidJavaObject activity, AndroidJavaObject contentResolver, AndroidJavaObject uri)
    {
        // Derive a filename from the URI display name, falling back to a timestamp.
        string fileName = GetDisplayName(contentResolver, uri)
                          ?? $"model_{System.DateTime.Now:yyyyMMdd_HHmmss}.glb";

        string destPath = System.IO.Path.Combine(Application.temporaryCachePath, fileName);

        try
        {
            using var inputStream = contentResolver.Call<AndroidJavaObject>("openInputStream", uri);
            if (inputStream == null)
            {
                Debug.LogError("[AndroidFilePicker] openInputStream returned null.");
                return string.Empty;
            }

            // Read all bytes through the Java stream.
            using var byteArrayOutputStream = new AndroidJavaObject("java.io.ByteArrayOutputStream");
            var buffer      = new byte[8192];
            int bytesRead;
            while ((bytesRead = inputStream.Call<int>("read", buffer)) != -1)
            {
                byteArrayOutputStream.Call("write", buffer, 0, bytesRead);
            }
            inputStream.Call("close");

            byte[] data = byteArrayOutputStream.Call<byte[]>("toByteArray");
            System.IO.File.WriteAllBytes(destPath, data);

            Debug.Log($"[AndroidFilePicker] Copied content URI to cache: '{destPath}' ({data.Length} bytes)");
            return destPath;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AndroidFilePicker] Failed to copy URI to cache: {e}");
            return string.Empty;
        }
    }

    private static string GetDisplayName(AndroidJavaObject contentResolver, AndroidJavaObject uri)
    {
        try
        {
            string[] projection = { "_display_name" };
            using var cursor = contentResolver.Call<AndroidJavaObject>(
                "query", uri, projection, null, null, null);

            if (cursor == null || !cursor.Call<bool>("moveToFirst"))
                return null;

            int colIndex = cursor.Call<int>("getColumnIndexOrThrow", "_display_name");
            string name  = cursor.Call<string>("getString", colIndex);
            cursor.Call("close");
            return name;
        }
        catch
        {
            return null;
        }
    }

    /// Ensures a persistent receiver GameObject exists in the scene.
    /// Android's UnitySendMessage needs a live object with the exact name.
    private static void EnsureReceiverExists()
    {
        if (GameObject.Find(ReceiverObjectName) != null)
            return;

        var go = new GameObject(ReceiverObjectName);
        go.AddComponent<AndroidFilePickerReceiver>();
        UnityEngine.Object.DontDestroyOnLoad(go);
    }
#endif
}

/// Receives the UnitySendMessage callback from the Android activity result.
/// Android calls: UnitySendMessage("AndroidFilePickerReceiver", "OnActivityResult", uriString)
public class AndroidFilePickerReceiver : MonoBehaviour
{
    // Called by Android via UnitySendMessage.
    public void OnActivityResult(string uriString)
    {
        Debug.Log($"[AndroidFilePickerReceiver] OnActivityResult: '{uriString}'");
        AndroidFilePicker.OnPickerResult(uriString);
    }
}