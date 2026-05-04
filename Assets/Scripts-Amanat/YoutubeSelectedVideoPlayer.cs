using LightShaft.Scripts;
using UnityEngine;

[RequireComponent(typeof(YoutubePlayer))]
public class YoutubeSelectedVideoPlayer : MonoBehaviour
{
    [Header("Plugin Player")]
    public YoutubePlayer youtubePlayer;

    [Header("Selection Playback")]
    public bool playSelectedVideoOnStart = true;
    public bool clearSelectionAfterStart = true;
    public bool keepInspectorUrlWhenNoSelection = true;

    private string selectedUrl;

    private void Awake()
    {
        if (youtubePlayer == null)
            youtubePlayer = GetComponent<YoutubePlayer>();

        selectedUrl = SelectedVideoData.videoIdToPlay;

        if (!string.IsNullOrWhiteSpace(selectedUrl) && youtubePlayer != null)
        {
            youtubePlayer.youtubeUrl = selectedUrl;
            youtubePlayer.autoPlayOnStart = playSelectedVideoOnStart;
        }
        else if (!keepInspectorUrlWhenNoSelection && youtubePlayer != null)
        {
            youtubePlayer.autoPlayOnStart = false;
        }
    }

    private void Start()
    {
        if (clearSelectionAfterStart && !string.IsNullOrWhiteSpace(selectedUrl))
            SelectedVideoData.Clear();
    }

    public void PlaySelectedVideo()
    {
        PlayUrl(SelectedVideoData.videoIdToPlay);
    }

    public void PlayUrl(string youtubeUrl)
    {
        if (string.IsNullOrWhiteSpace(youtubeUrl))
        {
            Debug.LogError("[YoutubeSelectedVideoPlayer] Cannot play an empty YouTube URL.");
            return;
        }

        if (youtubePlayer == null)
            youtubePlayer = GetComponent<YoutubePlayer>();

        if (youtubePlayer == null)
        {
            Debug.LogError("[YoutubeSelectedVideoPlayer] YoutubePlayer is missing.");
            return;
        }

        selectedUrl = youtubeUrl;
        SelectedVideoData.SetVideo(youtubeUrl, SelectedVideoData.videoTitleToPlay);
        youtubePlayer.youtubeUrl = youtubeUrl;
        youtubePlayer.Play(youtubeUrl);
    }
}
