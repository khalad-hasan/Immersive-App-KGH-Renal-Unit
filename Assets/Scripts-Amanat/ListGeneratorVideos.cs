using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class ListGeneratorVideos : MonoBehaviour
{
    [Header("Setup")]
    public MyItemView itemPrefab;
    public Transform container;
    public SceneChanger sceneChanger;
    public Sprite defaultSprite;

    [Header("Video Data Source")]
    public TopicDropdownLogger topicDropdown;

    [Header("Playback Target")]
    [Tooltip("Optional. Assign this when the list and Youtube360Player are in the same scene.")]
    public YoutubeSelectedVideoPlayer youtubePlayerBridge;
    public bool loadSceneAfterSelection = true;
    public int sceneChangerListIndex = 0;

    [Header("Generated Data")]
    public List<GameObject> spawnedItems = new List<GameObject>();

    void Start()
    {
        if (topicDropdown != null)
        {
            topicDropdown.onVideosFetched.AddListener(InitializeList);
        }
        else
        {
            Debug.LogWarning("TopicDropdownLogger is not assigned in ListGeneratorVideos!");
        }
    }

    public void InitializeList()
    {
        ClearList();

        if (topicDropdown.selectedFolder == null) return;

        int currentIndex = 0;

        foreach (var video in topicDropdown.selectedFolder.videos)
        {
            MyItemView newItem = Instantiate(itemPrefab, container);

            if (newItem.titleText != null)
                newItem.titleText.text = video.name;

            if (newItem.descriptionText != null)
                newItem.descriptionText.text = string.IsNullOrEmpty(video.description) ? "YouTube Video" : video.description;

            newItem.index = currentIndex;

            if (defaultSprite != null && newItem.iconImage != null)
            {
                newItem.iconImage.sprite = defaultSprite;
            }

            string thumbnailUrl = GetThumbnailUrl(video);
            if (!string.IsNullOrEmpty(thumbnailUrl) && newItem.iconImage != null)
            {
                StartCoroutine(DownloadAndApplySprite(thumbnailUrl, newItem.iconImage));
            }

            // Capture for closure
            string videoName = video.name;
            string videoUrl = topicDropdown.GetVideoUrl(video.file);

            newItem.actionButton.onClick.AddListener(() =>
            {
                PlaySelectedVideo(videoName, videoUrl);
            });

            spawnedItems.Add(newItem.gameObject);
            currentIndex++;
        }
    }

    private void PlaySelectedVideo(string videoName, string videoUrl)
    {
        if (string.IsNullOrEmpty(videoUrl))
        {
            Debug.LogError("[ListGeneratorVideos] Selected video has no URL.");
            return;
        }

        Debug.Log("Button Clicked! Video: " + videoName);
        SelectedVideoData.SetVideo(videoUrl, videoName);

        if (!loadSceneAfterSelection && youtubePlayerBridge != null)
        {
            youtubePlayerBridge.PlayUrl(videoUrl);
            return;
        }

        if (loadSceneAfterSelection)
        {
            if (sceneChanger != null)
            {
                sceneChanger.LoadSceneByIndex(sceneChangerListIndex);
            }
            else if (youtubePlayerBridge == null)
            {
                Debug.LogError("[ListGeneratorVideos] SceneChanger is missing and no YoutubeSelectedVideoPlayer is assigned.");
            }
        }
    }

    private string GetThumbnailUrl(TopicDropdownLogger.R2Video video)
    {
        if (!string.IsNullOrEmpty(video.thumbnail))
            return topicDropdown.GetThumbnailUrl(video.thumbnail);

        string youtubeId = ExtractYoutubeId(video.file);
        return string.IsNullOrEmpty(youtubeId) ? "" : $"https://img.youtube.com/vi/{youtubeId}/0.jpg";
    }

    private string ExtractYoutubeId(string url)
    {
        if (string.IsNullOrEmpty(url))
            return "";

        Match shortUrlMatch = Regex.Match(url, @"youtu\.be/([^?&/]+)", RegexOptions.IgnoreCase);
        if (shortUrlMatch.Success)
            return shortUrlMatch.Groups[1].Value;

        Match watchUrlMatch = Regex.Match(url, @"[?&]v=([^?&]+)", RegexOptions.IgnoreCase);
        if (watchUrlMatch.Success)
            return watchUrlMatch.Groups[1].Value;

        Match embedUrlMatch = Regex.Match(url, @"youtube\.com/embed/([^?&/]+)", RegexOptions.IgnoreCase);
        return embedUrlMatch.Success ? embedUrlMatch.Groups[1].Value : "";
    }

    private IEnumerator DownloadAndApplySprite(string imageUrl, Image targetImageComponent)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(uwr);

                Sprite newSprite = Sprite.Create(
                    downloadedTexture,
                    new Rect(0, 0, downloadedTexture.width, downloadedTexture.height),
                    new Vector2(0.5f, 0.5f)
                );

                targetImageComponent.sprite = newSprite;
            }
            else
            {
                Debug.LogWarning($"Failed to download thumbnail: {uwr.error}");
            }
        }
    }

    public void ClearList()
    {
        foreach (GameObject obj in spawnedItems)
        {
            if (obj != null) Destroy(obj);
        }

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        spawnedItems.Clear();
    }

    private void OnDestroy()
    {
        if (topicDropdown != null)
        {
            topicDropdown.onVideosFetched.RemoveListener(InitializeList);
        }
    }
}
