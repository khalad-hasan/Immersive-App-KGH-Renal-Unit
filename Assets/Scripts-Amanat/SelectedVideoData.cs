public static class SelectedVideoData
{
    public static string videoIdToPlay = "";
    public static string videoTitleToPlay = "";

    public static void SetVideo(string videoUrl, string videoTitle = "")
    {
        videoIdToPlay = videoUrl;
        videoTitleToPlay = videoTitle;
    }

    public static void Clear()
    {
        videoIdToPlay = "";
        videoTitleToPlay = "";
    }
}
