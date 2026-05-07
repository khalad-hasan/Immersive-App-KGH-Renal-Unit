using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Slider = UnityEngine.UI.Slider;

//This class control the youtube video player, you can make custom buttons and bind to that functions;
namespace LightShaft.Scripts
{
    public class YoutubeVideoController : MonoBehaviour
    {
        private YoutubePlayer _player;
        public bool showPlayerControl;
        public Slider playbackSlider;
        public Image progressRectangle;
        public Slider speedSlider;
        public Slider volumeSlider;
        public Text currentTime;
        public Text totalTime;
        public GameObject loading;
        public Button nextVideoButton;
        public Button previousVideoButton;

        #region Amanat 360 Video UI Extensions
        [Header("Amanat 360 Video Controls")]
        [Tooltip("RawImage on the play/pause button that receives the icon texture.")]
        public RawImage playbackIconTarget;
        [Tooltip("Texture shown when the video is paused and the next button press will play.")]
        public Texture playIconTexture;
        [Tooltip("Texture shown when the video is playing and the next button press will pause.")]
        public Texture pauseIconTexture;
        #endregion

        private bool showingVolume = false;
        private bool showingSpeed = false;

        [SerializeField]
        public GameObject controllerMainUI;

        [Tooltip("Time to auto hide the controller. 0 will not hide the controller.")]
        [SerializeField] public int hideScreenControlTime = 0;

        [Header("If you want to use a sprite rectangle instead of slider disable this")]
        public bool useSliderToProgressVideo = true;
        private void Awake()
        {
            _player = GetComponent<YoutubePlayer>();

            if (!showPlayerControl)
            {
                if(controllerMainUI != null)
                    controllerMainUI.SetActive(false);
                return;
            }
            if (!_player.customPlaylist)
            {
                if (previousVideoButton != null && nextVideoButton != null)
                {
                    previousVideoButton.gameObject.SetActive(false);
                    nextVideoButton.gameObject.SetActive(false);
                }
            }
            else
            {
                if (previousVideoButton != null && nextVideoButton != null)
                {
                    previousVideoButton.gameObject.SetActive(true);
                    nextVideoButton.gameObject.SetActive(true);
                }
            }
            
            if (showPlayerControl)
            {
                if(speedSlider == null)
                    Debug.LogWarning("Drag the playback speed slider to the speedSlider field.");
                if(volumeSlider == null)
                    Debug.LogWarning("Drag the volume eslider to the volumeSlider field.");
                if(playbackSlider == null)
                    Debug.LogWarning("Drag the playback slider to the playbackSlider field, this is necessary to change the video progress.");
            }
            speedSlider.maxValue = 3;   //max playback speed is 3;

            if (useSliderToProgressVideo)
            {
                progressRectangle.gameObject.SetActive(false);
                playbackSlider.gameObject.SetActive(true);
            }
            else
            {
                playbackSlider.gameObject.SetActive(false);
                progressRectangle.gameObject.SetActive(true);
            }
        }

        public void Play()
        {
            _player.Play();
            SetPlaybackIconPlaying();
        }

        public void Pause()
        {
            _player.Pause();
            SetPlaybackIconPaused();
        }

        public void PlayToggle()
        {
            if (_player == null || _player.videoPlayer == null)
                return;

            bool videoIsReady = _player.videoPlayer.isPrepared;
            _player.PlayPause();
            if (videoIsReady)
                UpdatePlaybackIconFromPlayerState();
        }

        #region Amanat 360 Video UI Extensions
        public void ReplayFromStart()
        {
            if (_player == null)
                _player = GetComponent<YoutubePlayer>();

            if (_player == null || _player.videoPlayer == null)
            {
                Debug.LogWarning("[YoutubeVideoController] Cannot replay because the YoutubePlayer is missing.");
                return;
            }

            if (!_player.videoPlayer.isPrepared)
            {
                Debug.LogWarning("[YoutubeVideoController] Cannot replay because the video is not ready yet.");
                return;
            }

            bool alreadyAtStart = _player.videoPlayer.time <= 0.05f;
            _player.pauseCalled = false;
            _player.progressStartDrag = false;

            if (playbackSlider != null)
                playbackSlider.value = 0;
            if (progressRectangle != null)
                progressRectangle.fillAmount = 0;

            _player.SkipToPercent(0f);
            if (alreadyAtStart)
                _player.Play();
            SetPlaybackIconPlaying();
        }

        private void UpdatePlaybackIconFromPlayerState()
        {
            if (_player == null)
                return;

            if (_player.pauseCalled)
                SetPlaybackIconPaused();
            else
                SetPlaybackIconPlaying();
        }

        private void SetPlaybackIconPlaying()
        {
            if (playbackIconTarget != null && pauseIconTexture != null)
                playbackIconTarget.texture = pauseIconTexture;
        }

        private void SetPlaybackIconPaused()
        {
            if (playbackIconTarget != null && playIconTexture != null)
                playbackIconTarget.texture = playIconTexture;
        }
        #endregion

        public void ChangeVolume(float volume)
        {
            switch (_player.videoPlayer.audioOutputMode)
            {
                case VideoAudioOutputMode.Direct:
                    _player.audioPlayer.SetDirectAudioVolume(0, volume);
                    _player.videoPlayer.SetDirectAudioVolume(0, volume);
                    break;
                case VideoAudioOutputMode.AudioSource:
                    _player.videoPlayer.GetComponent<AudioSource>().volume = volume;
                    _player.videoPlayer.SetDirectAudioVolume(0, volume);
                    break;
                default:
                    _player.videoPlayer.GetComponent<AudioSource>().volume = volume;
                    _player.videoPlayer.SetDirectAudioVolume(0, volume);
                    break;
            }
        }

        public void ChangePlaybackSpeed(float speed)
        {
            if(!_player.videoPlayer.canSetPlaybackSpeed) return;
            if (speed <= 0)
            {
                _player.videoPlayer.playbackSpeed = .5f;
                _player.audioPlayer.playbackSpeed = .5f;
            }
            else
            {
                _player.videoPlayer.playbackSpeed = speed;
                _player.audioPlayer.playbackSpeed = speed;
            }
           
        }

        public void PlayNextVideo()
        {
            if (!NextVideo())
                Debug.Log("Cannot play the next video.");
        }
        
        public void PlayPreviousVideo()
        {
            if(!PreviousVideo())
                Debug.Log("Cannot play the previous video.");
        }

        public bool NextVideo()
        {
            if (_player.customPlaylist)
            {
                _player.CallNextUrl();  return true;
            }else 
                return false;
        }

        public bool PreviousVideo()
        {
            if (_player.customPlaylist)
            {
                _player.CallPreviousUrl();
                return true;
            }else return false;
        }

        public void ChangeVideoTime(float value)
        {
            float pctg = (Mathf.RoundToInt(value) * 100) / playbackSlider.maxValue;
            pctg = pctg * 0.01f;
            _player.SkipToPercent(pctg);
            _player.progressStartDrag = false;
        }

        public void PlaybackSliderStartDrag()
        {
            _player.progressStartDrag = true;
        }

        public void ToggleFullScreen()
        {
            _player.ToogleFullsScreenMode();
        }
        
        public void HideControllers()
        {
            if (controllerMainUI != null)
            {
                controllerMainUI.SetActive(false);
                showingVolume = false;
                showingSpeed = false;
                volumeSlider.gameObject.SetActive(false);
                speedSlider.gameObject.SetActive(false);
            }
        }

        public void ToggleVolumeSlider()
        {
            if (showingVolume)
            {
                showingVolume = false;
                volumeSlider.gameObject.SetActive(false);
            }
            else
            {
                showingVolume = true;
                volumeSlider.gameObject.SetActive(true);
            }
        }

        public void ToggleSpeedSlider()
        {
            if (showingSpeed)
            {
                showingSpeed = false;
                speedSlider.gameObject.SetActive(false);
            }
            else
            {
                showingSpeed = true;
                speedSlider.gameObject.SetActive(true);
            }
        }
    }
}
