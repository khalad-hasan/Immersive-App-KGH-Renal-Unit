using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using TMPro;
using System.Text.RegularExpressions;
using System;
using Newtonsoft.Json.Linq;

[RequireComponent(typeof(TMP_Dropdown))]
public class TopicDropdownLogger : MonoBehaviour
{
    public enum VideoDataSource
    {
        R2Directory,
        FirebaseRealtimeDatabase
    }

    private TMP_Dropdown dropdown;
    public string topicSelected = "None";

    [Header("Data Source")]
    public VideoDataSource dataSource = VideoDataSource.R2Directory;

    [Header("R2 Server Settings")]
    [Tooltip("Paste your Cloudflare/Ngrok URL here. Make sure it ends with a slash /")]
    public string serverUrl = "https://cubic-crew-expansion-acquired.trycloudflare.com/";

    [Header("Firebase REST Settings")]
    [Tooltip("Example: https://your-project-id-default-rtdb.firebaseio.com/")]
    public string firebaseDatabaseUrl = "";
    [Tooltip("Database child/path that contains your categories. Example: Category")]
    public string firebaseRootChild = "Category";
    [Tooltip("Optional auth token. Leave empty when database rules allow reads.")]
    public string firebaseAuthToken = "";
    [Tooltip("When a video child is an object, this field should contain the YouTube URL.")]
    public string firebaseVideoUrlField = "url";
    public string firebaseTitleField = "title";
    public string firebaseDescriptionField = "description";
    public string firebaseThumbnailField = "thumbnail";

    [Header("Events")]
    public UnityEvent onFoldersFetched;
    public UnityEvent onVideosFetched;

    [Header("Startup Player Position")]
    [Tooltip("Assign the XR Origin/player root here. If empty, the script uses Camera.main's root transform.")]
    public Transform playerTransform;
    [Tooltip("Assign the TeleportAnchor GameObject transform here. Only its X and Z position are used.")]
    public Transform startupPositionAnchor;
    public bool movePlayerToAnchorOnStart = true;
    [Tooltip("Small delay gives the XR Origin time to initialize before the position is changed.")]
    public float startupPositionDelay = 0.25f;
    [Tooltip("Repeats the startup move briefly so XR tracking/locomotion startup cannot overwrite it.")]
    public float startupPositionRetryDuration = 2f;

    [HideInInspector] public List<R2Folder> folders = new List<R2Folder>();
    [HideInInspector] public R2Folder selectedFolder;

    [System.Serializable]
    public class R2Video
    {
        public string name;
        public string file;
        public string thumbnail;
        public string url;
        public string description;
    }

    [System.Serializable]
    public class R2Folder
    {
        public string name;
        public List<R2Video> videos;
    }

    void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        dropdown.onValueChanged.AddListener(OnDropdownChanged);

        if (!serverUrl.EndsWith("/")) serverUrl += "/";

        if (movePlayerToAnchorOnStart)
            StartCoroutine(MovePlayerToStartupAnchorAfterDelay());

        StartCoroutine(FetchFolders());
    }

    public void RequestStartupTeleport()
    {
        MovePlayerToStartupAnchor();
    }

    public void MovePlayerToStartupAnchor()
    {
        Transform targetPlayerTransform = playerTransform;
        if (targetPlayerTransform == null && Camera.main != null)
            targetPlayerTransform = Camera.main.transform.root;

        if (targetPlayerTransform == null)
        {
            Debug.LogWarning("[TopicDropdown] Player transform is not assigned and no main camera was found.");
            return;
        }

        if (startupPositionAnchor == null)
        {
            Debug.LogWarning("[TopicDropdown] Startup position anchor is not assigned.");
            return;
        }

        Vector3 currentPosition = targetPlayerTransform.position;
        Vector3 anchorPosition = startupPositionAnchor.position;
        Camera mainCamera = Camera.main;

        if (mainCamera != null && mainCamera.transform.IsChildOf(targetPlayerTransform))
        {
            Vector3 cameraPosition = mainCamera.transform.position;
            Vector3 cameraToAnchorOffset = new Vector3(
                anchorPosition.x - cameraPosition.x,
                0f,
                anchorPosition.z - cameraPosition.z
            );

            targetPlayerTransform.position = currentPosition + cameraToAnchorOffset;
        }
        else
        {
            targetPlayerTransform.position = new Vector3(anchorPosition.x, currentPosition.y, anchorPosition.z);
        }
    }

    private IEnumerator MovePlayerToStartupAnchorAfterDelay()
    {
        if (startupPositionDelay > 0f)
            yield return new WaitForSeconds(startupPositionDelay);
        else
            yield return null;

        float endTime = Time.unscaledTime + Mathf.Max(0f, startupPositionRetryDuration);
        do
        {
            MovePlayerToStartupAnchor();
            yield return null;
        }
        while (Time.unscaledTime < endTime);
    }

    public void Refresh()
    {
        StopAllCoroutines();
        StartCoroutine(FetchFolders());
    }

    private IEnumerator FetchFolders()
    {
        if (dataSource == VideoDataSource.FirebaseRealtimeDatabase)
        {
            yield return StartCoroutine(FetchFoldersFromFirebase());
        }
        else
        {
            yield return StartCoroutine(FetchFoldersFromServer());
        }
    }

    private IEnumerator FetchFoldersFromServer()
    {
        Debug.Log($"[TopicDropdown] Fetching directory list from: {serverUrl}");

        using (UnityWebRequest req = UnityWebRequest.Get(serverUrl))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[TopicDropdown] Failed to connect to server: {req.error}");
                yield break;
            }

            folders.Clear();
            string htmlContent = req.downloadHandler.text;

            MatchCollection matches = Regex.Matches(htmlContent, @"href=""([^""]+)""");

            foreach (Match match in matches)
            {
                string rawName = match.Groups[1].Value;
                string cleanName = UnityWebRequest.UnEscapeURL(rawName);

                if (cleanName.EndsWith("/") && rawName != "../" && rawName != "/")
                {
                    string folderName = cleanName.TrimEnd('/');
                    folders.Add(new R2Folder { name = folderName, videos = new List<R2Video>() });
                }
            }

            yield return StartCoroutine(FetchVideosForFolders());
        }
    }

    private IEnumerator FetchVideosForFolders()
    {
        foreach (var folder in folders)
        {
            string escapedFolder = UnityWebRequest.EscapeURL(folder.name).Replace("+", "%20");
            string folderUrl = serverUrl + escapedFolder + "/";

            using (UnityWebRequest req = UnityWebRequest.Get(folderUrl))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string htmlContent = req.downloadHandler.text;
                    MatchCollection matches = Regex.Matches(htmlContent, @"href=""([^""]+)""");

                    foreach (Match match in matches)
                    {
                        string rawName = match.Groups[1].Value;
                        string cleanName = UnityWebRequest.UnEscapeURL(rawName);

                        if (cleanName.EndsWith(".mp4", System.StringComparison.OrdinalIgnoreCase))
                        {
                            folder.videos.Add(new R2Video
                            {
                                name = cleanName.Replace(".mp4", ""),
                                file = cleanName,
                                // Assumes a .png exists with the exact same name as the .mp4
                                thumbnail = cleanName.Replace(".mp4", ".png", System.StringComparison.OrdinalIgnoreCase)
                            });
                        }
                    }
                }
            }
        }

        PopulateDropdown();
        onFoldersFetched?.Invoke();
    }

    private IEnumerator FetchFoldersFromFirebase()
    {
        if (string.IsNullOrEmpty(firebaseDatabaseUrl))
        {
            Debug.LogError("[TopicDropdown] Firebase Database URL is empty.");
            yield break;
        }

        string requestUrl = BuildFirebaseRestUrl(firebaseRootChild);
        Debug.Log($"[TopicDropdown] Fetching Firebase videos from: {requestUrl}");

        using (UnityWebRequest req = UnityWebRequest.Get(requestUrl))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[TopicDropdown] Failed to connect to Firebase: {req.error}");
                yield break;
            }

            folders.Clear();
            string json = req.downloadHandler.text;

            if (string.IsNullOrWhiteSpace(json) || json == "null")
            {
                Debug.LogWarning($"[TopicDropdown] Firebase path '{firebaseRootChild}' is empty.");
                PopulateDropdown();
                onFoldersFetched?.Invoke();
                yield break;
            }

            JToken rootToken;
            try
            {
                rootToken = JToken.Parse(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TopicDropdown] Firebase JSON could not be parsed: {ex.Message}");
                yield break;
            }

            if (LooksLikeFirebaseVideoCollection(rootToken))
            {
                List<R2Video> videos = ParseFirebaseVideos(rootToken);
                if (videos.Count > 0)
                {
                    folders.Add(new R2Folder
                    {
                        name = GetFirebasePathLeaf(firebaseRootChild),
                        videos = videos
                    });
                }
            }
            else if (rootToken.Type == JTokenType.Object)
            {
                foreach (JProperty subject in ((JObject)rootToken).Properties())
                {
                    List<R2Video> videos = ParseFirebaseVideos(subject.Value);
                    if (videos.Count == 0)
                        continue;

                    folders.Add(new R2Folder
                    {
                        name = subject.Name,
                        videos = videos
                    });
                }
            }

            PopulateDropdown();
            onFoldersFetched?.Invoke();
        }
    }

    private void PopulateDropdown()
    {
        dropdown.ClearOptions();

        List<string> names = new List<string>();
        foreach (var folder in folders)
        {
            names.Add(folder.name);
        }

        dropdown.AddOptions(names);

        if (names.Count > 0)
        {
            dropdown.value = 0;
            dropdown.RefreshShownValue();
            OnDropdownChanged(0);
        }
        else
        {
            selectedFolder = null;
            topicSelected = "None";
        }
    }

    private void OnDropdownChanged(int index)
    {
        if (index < 0 || index >= folders.Count) return;

        selectedFolder = folders[index];
        topicSelected = selectedFolder.name;

        onVideosFetched?.Invoke();
    }

    public string GetVideoUrl(string fileName)
    {
        if (dataSource == VideoDataSource.FirebaseRealtimeDatabase)
            return fileName;

        if (IsAbsoluteWebUrl(fileName))
            return fileName;

        string escapedFolder = UnityWebRequest.EscapeURL(topicSelected).Replace("+", "%20");
        string escapedFile = UnityWebRequest.EscapeURL(fileName).Replace("+", "%20");
        return $"{serverUrl}{escapedFolder}/{escapedFile}";
    }

    public string GetThumbnailUrl(string thumbnailName)
    {
        if (IsAbsoluteWebUrl(thumbnailName))
            return thumbnailName;

        if (dataSource == VideoDataSource.FirebaseRealtimeDatabase)
            return thumbnailName;

        // Now it properly constructs the URL for the .png file so ListGeneratorVideos can download it
        string escapedFolder = UnityWebRequest.EscapeURL(topicSelected).Replace("+", "%20");
        string escapedFile = UnityWebRequest.EscapeURL(thumbnailName).Replace("+", "%20");
        return $"{serverUrl}{escapedFolder}/{escapedFile}";
    }

    private string BuildFirebaseRestUrl(string path)
    {
        string baseUrl = firebaseDatabaseUrl.TrimEnd('/');
        string cleanPath = string.IsNullOrEmpty(path) ? "" : path.Trim('/');
        string url = string.IsNullOrEmpty(cleanPath)
            ? $"{baseUrl}/.json"
            : $"{baseUrl}/{EscapeFirebasePath(cleanPath)}.json";

        if (!string.IsNullOrEmpty(firebaseAuthToken))
            url += "?auth=" + UnityWebRequest.EscapeURL(firebaseAuthToken);

        return url;
    }

    private string EscapeFirebasePath(string path)
    {
        string[] parts = path.Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = UnityWebRequest.EscapeURL(parts[i]).Replace("+", "%20");
        }

        return string.Join("/", parts);
    }

    private bool LooksLikeFirebaseVideoCollection(JToken token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return false;

        if (IsFirebaseVideoEntry(token))
            return true;

        if (token.Type == JTokenType.Array)
            return true;

        if (token.Type != JTokenType.Object)
            return false;

        int inspectedChildren = 0;
        int videoChildren = 0;

        foreach (JProperty child in ((JObject)token).Properties())
        {
            if (child.Value.Type == JTokenType.Null)
                continue;

            inspectedChildren++;

            if (IsFirebaseVideoEntry(child.Value))
                videoChildren++;
        }

        return inspectedChildren > 0 && inspectedChildren == videoChildren;
    }

    private bool IsFirebaseVideoEntry(JToken token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return false;

        if (token.Type == JTokenType.String)
            return !string.IsNullOrWhiteSpace(token.Value<string>());

        if (token.Type != JTokenType.Object)
            return false;

        return !string.IsNullOrEmpty(ReadStringField((JObject)token, firebaseVideoUrlField, "url", "youtubeUrl", "youtubeURL", "videoUrl", "videoURL", "link"));
    }

    private List<R2Video> ParseFirebaseVideos(JToken token)
    {
        List<R2Video> videos = new List<R2Video>();

        if (token == null || token.Type == JTokenType.Null)
            return videos;

        if (IsFirebaseVideoEntry(token))
        {
            R2Video singleVideo = CreateFirebaseVideo("Video", token);
            if (singleVideo != null)
                videos.Add(singleVideo);

            return videos;
        }

        if (token.Type == JTokenType.Object)
        {
            foreach (JProperty videoEntry in ((JObject)token).Properties())
            {
                R2Video video = CreateFirebaseVideo(videoEntry.Name, videoEntry.Value);
                if (video != null)
                    videos.Add(video);
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            int index = 1;
            foreach (JToken child in token.Children())
            {
                R2Video video = CreateFirebaseVideo($"Video {index}", child);
                if (video != null)
                    videos.Add(video);

                index++;
            }
        }

        return videos;
    }

    private R2Video CreateFirebaseVideo(string key, JToken token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return null;

        string url = "";
        string title = HumanizeFirebaseKey(key);
        string description = "";
        string thumbnail = "";

        if (token.Type == JTokenType.String)
        {
            url = token.Value<string>();
        }
        else if (token.Type == JTokenType.Object)
        {
            JObject obj = (JObject)token;
            url = ReadStringField(obj, firebaseVideoUrlField, "url", "youtubeUrl", "youtubeURL", "videoUrl", "videoURL", "link");
            title = ReadStringField(obj, firebaseTitleField, "title", "name");
            description = ReadStringField(obj, firebaseDescriptionField, "description", "desc");
            thumbnail = ReadStringField(obj, firebaseThumbnailField, "thumbnail", "thumb", "image");

            if (string.IsNullOrEmpty(title))
                title = HumanizeFirebaseKey(key);
        }

        if (string.IsNullOrWhiteSpace(url))
            return null;

        return new R2Video
        {
            name = title,
            file = url,
            url = url,
            thumbnail = thumbnail,
            description = description
        };
    }

    private string ReadStringField(JObject obj, params string[] fieldNames)
    {
        foreach (string fieldName in fieldNames)
        {
            if (string.IsNullOrEmpty(fieldName))
                continue;

            foreach (JProperty property in obj.Properties())
            {
                if (!string.Equals(property.Name, fieldName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (property.Value == null || property.Value.Type == JTokenType.Null)
                    return "";

                return property.Value.Type == JTokenType.String
                    ? property.Value.Value<string>()
                    : property.Value.ToString();
            }
        }

        return "";
    }

    private string GetFirebasePathLeaf(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "Videos";

        string cleanPath = path.Trim('/');
        int slashIndex = cleanPath.LastIndexOf('/');
        return slashIndex >= 0 ? cleanPath.Substring(slashIndex + 1) : cleanPath;
    }

    private string HumanizeFirebaseKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "Video";

        return UnityWebRequest.UnEscapeURL(key).Replace("_", " ").Replace("-", " ");
    }

    private bool IsAbsoluteWebUrl(string value)
    {
        return !string.IsNullOrEmpty(value)
               && (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                   || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
    }

    private void OnDestroy()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }
}
