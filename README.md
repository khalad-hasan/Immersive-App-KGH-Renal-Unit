# U6.3 360 Screen Project Notes

This project uses two main scenes. The first scene loads video data and builds the selectable UI. The second scene plays the selected YouTube 360 video through the LightShaft YouTube player.

## Important Scenes

### `Assets/Scenes/0StartScreen.unity`
Main menu and video browser scene. It reads categories/videos, fills the vertical list, stores the selected YouTube URL, then loads the 360 playback scene.

### `Assets/LightShaft/Scenes/Demo6 - 360 playback.unity`
Main 360 YouTube playback scene. The `Youtube360Player` GameObject uses the LightShaft player scripts and receives the selected URL through `YoutubeSelectedVideoPlayer`.

## Scene Flow

1. `TopicDropdownLogger` fetches categories and video links.
2. `ListGeneratorVideos` creates one button per video.
3. When a button is clicked, `SelectedVideoData` stores the selected YouTube URL.
4. `SceneChanger` loads `Demo6 - 360 playback`.
5. `YoutubeSelectedVideoPlayer` gives the stored URL to `YoutubePlayer`.

## Main Project Scripts

### `Assets/Scripts-Amanat/TopicDropdownLogger.cs`
Fetches video categories and video records. Supports the old R2 directory source and the current Firebase Realtime Database REST source. Fires events when folders or videos are ready.

### `Assets/Scripts-Amanat/ListGeneratorVideos.cs`
Builds the vertical video list from the selected category. Creates UI items, downloads thumbnails when available, handles button clicks, stores the selected URL, and optionally loads the playback scene.

### `Assets/Scripts-Amanat/MyItemView.cs`
Small UI holder for each generated list item. Stores references to title, description, price text, button, icon image, and item index.

### `Assets/Scripts-Amanat/SelectedVideoData.cs`
Static handoff object between scenes. Stores the selected YouTube URL and title before scene load, then can clear them after playback scene startup.

### `Assets/Scripts-Amanat/YoutubeSelectedVideoPlayer.cs`
Bridge between Amanat scripts and the LightShaft plugin. Reads `SelectedVideoData`, assigns the URL to `YoutubePlayer`, and can play a selected URL directly.

### `Assets/Scripts-Amanat/SceneChanger.cs`
Simple scene loading helper. Uses an Inspector list of scene names, supports loading by index, loading by name, restarting the current scene, and returning to the main menu.

### `Assets/Scripts-Amanat/VideoManager.cs`
[Not used anymore] Previous implementation of local/R2 video playback script. Downloads MP4 files, caches them locally, and plays them through Unity `VideoPlayer`. Not part of the current YouTube plugin path.

## Main LightShaft Plugin Scripts

### `Assets/LightShaft/Scripts/YoutubePlayer.cs`
Main plugin entry point for YouTube playback. Public `Play(string url)` loads and plays a YouTube URL. Keep this script as plugin-owned unless a plugin fix is required.

### `Assets/LightShaft/Scripts/YoutubeVideoController.cs`
Controls playback UI behavior for the LightShaft player, including play, pause, volume, speed, progress seeking, fullscreen toggle, and previous/next playlist buttons.

### `Assets/LightShaft/Scripts/YoutubeVideoEvents.cs`
Defines Unity events for YouTube playback lifecycle. Exposes callbacks for URL ready, video ready, started, paused, resumed, finished, and timed video events.

## Firebase Data Shape


Link to Firebase Database: https://console.firebase.google.com/u/2/project/interiorhealth-vr-videos/database/interiorhealth-vr-videos-default-rtdb/data


Use `Category` as the Firebase root child unless the Inspector value is changed.

```json
{
  "Category": {
    "Subject One": {
      "Video One": "https://www.youtube.com/watch?v=...",
      "Video Two": {
        "title": "My 360 Video",
        "url": "https://youtu.be/...",
        "description": "Optional text",
        "thumbnail": "https://..."
      }
    }
  }
}
```

## Build Notes

Only these scenes are enabled in Build Settings:

- `Assets/Scenes/0StartScreen.unity`
- `Assets/LightShaft/Scenes/Demo6 - 360 playback.unity`


