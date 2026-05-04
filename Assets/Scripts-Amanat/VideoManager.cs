using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.IO;
using System.Collections;

[RequireComponent(typeof(VideoPlayer))]
public class VideoManager : MonoBehaviour
{
    [Header("Playback UI Controls")]
    public RawImage playbackIconTarget;
    public Texture playIconTexture;
    public Texture pauseIconTexture;

    [Header("Video Settings")]
    public VideoPlayer videoPlayer;

    [Header("Download UI")]
    public TextMeshProUGUI downloadProgressText;
    public GameObject Panel;

    private string saveDirectory;

    void Start()
    {
        Panel.SetActive(false);
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        videoPlayer.Stop();
        videoPlayer.url = "";

        if (downloadProgressText != null)
        {
            downloadProgressText.gameObject.SetActive(false);
            if (Panel != null) Panel.SetActive(false);
        }

        saveDirectory = Path.Combine(Application.persistentDataPath, "Downloaded360Videos");
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        if (!string.IsNullOrEmpty(SelectedVideoData.videoIdToPlay))
        {
            StartCoroutine(DownloadAndPlayRoutine(SelectedVideoData.videoIdToPlay));
            SelectedVideoData.videoIdToPlay = "";
        }
        else
        {
            Debug.LogWarning("Video URL is empty!");
        }
    }

    private IEnumerator DownloadAndPlayRoutine(string r2Url)
    {
        // Use the filename from the URL as the cache key
        string fileName = Path.GetFileName(UnityWebRequest.UnEscapeURL(r2Url));
        string localFilePath = Path.Combine(saveDirectory, fileName);

        if (File.Exists(localFilePath))
        {
            FileInfo info = new FileInfo(localFilePath);
            if (info.Length < 100000)
            {
                File.Delete(localFilePath);
            }
            else
            {
                Debug.Log($"[VideoManager] Video found locally! Playing from cache.");
                PrepareAndPlay(localFilePath);
                yield break;
            }
        }

        Debug.Log($"[VideoManager] Downloading from R2: {r2Url}");

        if (Panel != null) Panel.SetActive(true);
        if (downloadProgressText != null)
        {
            downloadProgressText.gameObject.SetActive(true);
            downloadProgressText.text = "Starting download...";
        }

        System.GC.Collect();
        yield return new WaitForSeconds(0.5f);

        UnityWebRequest uwr = UnityWebRequest.Get(r2Url);
        uwr.downloadHandler = new DownloadHandlerFile(localFilePath);
        uwr.disposeDownloadHandlerOnDispose = true;

        var operation = uwr.SendWebRequest();
        while (!operation.isDone)
        {
            if (downloadProgressText != null)
                downloadProgressText.text = $"Downloading: {(uwr.downloadProgress * 100):F0}%";
            yield return null;
        }

        if (Panel != null) Panel.SetActive(false);
        if (downloadProgressText != null) downloadProgressText.gameObject.SetActive(false);

        bool success = uwr.result == UnityWebRequest.Result.Success;
        string error = uwr.error;

        uwr.downloadHandler?.Dispose();
        uwr.Dispose();
        uwr = null;

        yield return null;
        System.GC.Collect();
        yield return null;

        if (!success)
        {
            Debug.LogError($"[VideoManager] R2 Download Failed: {error}");
            if (File.Exists(localFilePath))
            {
                try { Debug.LogError($"[VideoManager] Error body: {File.ReadAllText(localFilePath)}"); } catch { }
                File.Delete(localFilePath);
            }
        }
        else
        {
            Debug.Log("[VideoManager] Download complete! Playing video now.");
            PrepareAndPlay(localFilePath);
        }
    }

    private void PrepareAndPlay(string filePath)
    {
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = filePath;
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("[VideoManager] Video buffered successfully. Playing...");
        PlayVideo();
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"[VideoManager] Unity Video Player Error: {message}");
    }

    public void PlayVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Play();
            if (playbackIconTarget != null && pauseIconTexture != null)
                playbackIconTarget.texture = pauseIconTexture;
        }
    }

    public void PauseVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Pause();
            if (playbackIconTarget != null && playIconTexture != null)
                playbackIconTarget.texture = playIconTexture;
        }
    }

    public void ReplayVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.Play();
            if (playbackIconTarget != null && pauseIconTexture != null)
                playbackIconTarget.texture = pauseIconTexture;
        }
    }

    public void TogglePlayPause()
    {
        if (videoPlayer != null)
        {
            if (videoPlayer.isPlaying)
                PauseVideo();
            else
                PlayVideo();
        }
    }

    private void OnDisable()
    {
        ForceCleanup();
    }

    private void OnDestroy()
    {
        ForceCleanup();
    }

    private void ForceCleanup()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.url = "";
        }
    }
}