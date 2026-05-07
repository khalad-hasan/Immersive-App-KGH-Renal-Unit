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

This script also handles the start-scene player position reset for `0StartScreen`. It moves the XR Origin/player so the headset camera's X/Z position matches the assigned startup anchor. It also listens for Meta/Oculus recenter events and reapplies the same correction after the headset tracking origin changes.

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

### `Assets/Scripts-Amanat/ResetPosition.cs`
Standalone position reset helper for the 360 playback scene. Add it to a GameObject in `Demo6 - 360 playback`, assign the XR Origin/player root and the target anchor transform, and it will move the headset camera X/Z to that anchor. It also handles Meta/Oculus recenter by reapplying the correction when the XR tracking origin changes.

### `Assets/Scripts-Amanat/VideoManager.cs`
[Not used anymore] Previous implementation of local/R2 video playback script. Downloads MP4 files, caches them locally, and plays them through Unity `VideoPlayer`. Not part of the current YouTube plugin path.

## Main LightShaft Plugin Scripts

### `Assets/LightShaft/Scripts/YoutubePlayer.cs`
Main plugin entry point for YouTube playback. Public `Play(string url)` loads and plays a YouTube URL. Keep this script as plugin-owned unless a plugin fix is required.

### `Assets/LightShaft/Scripts/YoutubeVideoController.cs`
Controls playback UI behavior for the LightShaft player, including play, pause, replay, play/pause icon switching, volume, speed, progress seeking, fullscreen toggle, and previous/next playlist buttons.

This is the main place to add current and future 360 video UI behavior. For new playback controls, add the public method here, then call it from the Inspector on the relevant UI element.

Current custom controls added for this project are grouped in the `Amanat 360 Video UI Extensions` region:

- `playbackIconTarget`: the `RawImage` on the play/pause button.
- `playIconTexture`: texture shown when the video is paused and the button should play.
- `pauseIconTexture`: texture shown when the video is playing and the button should pause.
- `PlayToggle()`: toggles play/pause and updates the button texture.
- `ReplayFromStart()`: seeks the current YouTube video back to the start and resumes playback.
- Existing timeline methods such as `PlaybackSliderStartDrag()` and `ChangeVideoTime(float value)` should be used for the video seek slider.

Inspector wiring note: in the 360 playback scene, select the UI element's `Text Poke Button`, then add/call the needed `YoutubeVideoController` method from its Inspector event. This keeps button behavior editable in Unity without changing scene flow scripts.

## 360 Scene UI Workflow

The active 360 playback controls should be managed through `YoutubeVideoController.cs`. Add new public methods there when a button, slider, or other UI element needs to control video playback.

For scene wiring, use the working UI setup that contains the interactive playback buttons. The previous separate slider UI was not interactable because it was tied to another UI/EventSystem setup without the required Quest interaction components. The current recommended approach is:

- Place playback buttons and the video timeline slider on the same working UI setup.
- Wire button actions through each element's `Text Poke Button` Inspector events.
- Wire the seek slider to `YoutubeVideoController.PlaybackSliderStartDrag()` and `YoutubeVideoController.ChangeVideoTime(float value)`.
- Avoid adding another EventSystem for playback controls unless there is a specific reason.

The earlier timeline/seek-slider item is no longer considered future work; it should be treated as part of the current 360 UI workflow.

## Player Position Reset

Both scenes use the same reset strategy: move the XR Origin/player root so the actual headset camera lands on a known anchor's X/Z position, while keeping the current Y height unchanged. This is more reliable than calling `TeleportationAnchor.RequestTeleport()` because that method depends on the XR teleport provider and interaction state.

### Scene 1: `Assets/Scenes/0StartScreen.unity`
Uses `Assets/Scripts-Amanat/TopicDropdownLogger.cs`.

Inspector fields:

- `Player Transform`: assign `XR Origin Hands (XR Rig)` or the active XR Origin/player root.
- `Startup Position Anchor`: assign the start-scene anchor/TeleportAnchor transform.
- `Move Player To Anchor On Start`: keep enabled.
- `Reset After Tracking Origin Change`: keep enabled so Meta/Oculus recenter is corrected automatically.

### Scene 2: `Assets/LightShaft/Scenes/Demo6 - 360 playback.unity`
Uses `Assets/Scripts-Amanat/ResetPosition.cs`.

Inspector fields:

- `Player Transform`: assign `XR Origin Hands (XR Rig)` or the active XR Origin/player root.
- `Position Anchor`: assign the playback-scene anchor/TeleportAnchor transform.
- `Reset Position On Start`: keep enabled.
- `Reset After Tracking Origin Change`: keep enabled so Meta/Oculus recenter is corrected automatically.

Important behavior: if the user presses the Meta/Oculus recenter button, Unity updates the XR tracking origin. Both scripts listen for that tracking-origin update and reapply the same X/Z correction shortly after, so the scene should stay aligned.

### `Assets/LightShaft/Scripts/YoutubeVideoEvents.cs`
Defines Unity events for YouTube playback lifecycle. Exposes callbacks for URL ready, video ready, started, paused, resumed, finished, and timed video events.

## Firebase Data Shape


Link to Firebase Database: https://console.firebase.google.com/u/0/project/interiorhealth-vr-videos/database/interiorhealth-vr-videos-default-rtdb/data


Use `Category` as the Firebase root child unless the Inspector value is changed.

You have two way to provide the link. 
Option 1: you see "Video One" Directly provides the link. The information is fetched directly from the video. 
Option 2: you see "Video Two" you can customize the info. 


```json
{
  "Category": {
    "Subject One": {
      "Video One": "https://www.youtube.com/watch?v=...",
      "Video Two": {
        "title": "My 360 Video",
        "url": "https://youtu.be/...",
        "desc": "Optional text",
        "thumb": "Optional Custom Thumbnail"
      }
    }
  }
}
```

## Build Notes

Only these scenes are enabled in Build Settings:

- `Assets/Scenes/0StartScreen.unity`
- `Assets/LightShaft/Scenes/Demo6 - 360 playback.unity`


