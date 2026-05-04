using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class GoogleDriveFetcher : MonoBehaviour
{
    [Header("Google Drive API Settings")]
    public string googleDriveApiKey = "";
    [Tooltip("The ID of the main public folder containing your subfolders")]
    public string rootFolderId = "";

    [System.Serializable]
    public class MediaPair
    {
        public string title;
        public string videoFileId;
        public string imageFileId;
    }

    [Header("Fetched Data (Read-Only)")]
    public List<DriveFile> availableFolders = new List<DriveFile>();
    public List<MediaPair> availableMedia = new List<MediaPair>();

    [Header("Events For Your UI Scripts")]
    [Tooltip("Fires when the folder list finishes downloading")]
    public UnityEvent onFoldersFetched;
    [Tooltip("Fires when the video list finishes downloading")]
    public UnityEvent onVideosFetched;

    private const string DriveApiUrl = "https://www.googleapis.com/drive/v3/files";

    [System.Serializable]
    public class DriveFile
    {
        public string id;
        public string name;
        public string mimeType;
    }

    [System.Serializable]
    private class DriveFileList
    {
        public List<DriveFile> files;
    }

    public void FetchFolders()
    {
        StartCoroutine(GetFoldersRoutine(rootFolderId));
    }

    public void FetchVideosInFolder(string selectedFolderId)
    {
        StartCoroutine(GetVideosRoutine(selectedFolderId));
    }

    private IEnumerator GetFoldersRoutine(string parentId)
    {
        string query = $"mimeType='application/vnd.google-apps.folder' and '{parentId}' in parents and trashed=false";
        string url = $"{DriveApiUrl}?q={UnityWebRequest.EscapeURL(query)}&fields=files(id,name,mimeType)&pageSize=100&key={googleDriveApiKey}";

        yield return StartCoroutine(MakeDriveApiRequest(url, (jsonResponse) =>
        {
            DriveFileList folderList = JsonUtility.FromJson<DriveFileList>(jsonResponse);
            availableFolders = folderList.files;
            onFoldersFetched?.Invoke();
        }));
    }

    private IEnumerator GetVideosRoutine(string folderId)
    {
        string query = $"(mimeType='text/plain' or mimeType contains 'image/') and '{folderId}' in parents and trashed=false";
        string url = $"{DriveApiUrl}?q={UnityWebRequest.EscapeURL(query)}&fields=files(id,name,mimeType)&pageSize=100&key={googleDriveApiKey}";

        yield return StartCoroutine(MakeDriveApiRequest(url, (jsonResponse) =>
        {
            DriveFileList fileList = JsonUtility.FromJson<DriveFileList>(jsonResponse);

            Dictionary<string, string> videoDict = new Dictionary<string, string>();
            Dictionary<string, string> imageDict = new Dictionary<string, string>();

            foreach (var file in fileList.files)
            {
                string cleanName = System.IO.Path.GetFileNameWithoutExtension(file.name);

                if (file.mimeType.Contains("text"))
                {
                    videoDict[cleanName] = file.id;
                }
                else if (file.mimeType.Contains("image"))
                {
                    imageDict[cleanName] = file.id;
                }
            }

            availableMedia.Clear();

            foreach (var kvp in videoDict)
            {
                string matchedImageId = imageDict.ContainsKey(kvp.Key) ? imageDict[kvp.Key] : "";
                availableMedia.Add(new MediaPair
                {
                    title = kvp.Key,
                    videoFileId = kvp.Value,
                    imageFileId = matchedImageId
                });
            }

            onVideosFetched?.Invoke();
        }));
    }

    private IEnumerator MakeDriveApiRequest(string url, System.Action<string> onSuccess)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[GoogleDriveFetcher] API Error: {webRequest.error}\nResponse: {webRequest.downloadHandler.text}");
            }
            else
            {
                onSuccess?.Invoke(webRequest.downloadHandler.text);
            }
        }
    }
}