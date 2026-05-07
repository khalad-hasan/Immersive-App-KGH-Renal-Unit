using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ResetPosition : MonoBehaviour
{
    [Header("Position References")]
    [Tooltip("Assign the XR Origin/player root here. If empty, the script uses Camera.main's root transform.")]
    public Transform playerTransform;
    [Tooltip("Assign the target anchor transform here. Only its X and Z position are used.")]
    public Transform positionAnchor;

    [Header("Startup Reset")]
    public bool resetPositionOnStart = true;
    [Tooltip("Small delay gives the XR Origin time to initialize before the position is changed.")]
    public float startupDelay = 0.25f;
    [Tooltip("Repeats the startup move briefly so XR tracking/locomotion startup cannot overwrite it.")]
    public float startupRetryDuration = 2f;

    [Header("Meta/Oculus Recenter")]
    [Tooltip("Re-apply the same position correction when the headset tracking origin is reset.")]
    public bool resetAfterTrackingOriginChange = true;
    [Tooltip("Small delay lets the headset finish applying its recenter before this script corrects the scene position.")]
    public float trackingOriginResetDelay = 0.1f;
    [Tooltip("Repeats the correction briefly after recenter so the final headset position stays on the anchor.")]
    public float trackingOriginRetryDuration = 1f;

    private readonly List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
    private Coroutine resetRoutine;

    private void OnEnable()
    {
        RegisterTrackingOriginCallbacks();
    }

    private void OnDisable()
    {
        UnregisterTrackingOriginCallbacks();
        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
            resetRoutine = null;
        }
    }

    private void Start()
    {
        RegisterTrackingOriginCallbacks();

        if (resetPositionOnStart)
            StartResetRoutine(startupDelay, startupRetryDuration);
    }

    public void ResetPlayerPosition()
    {
        Transform targetPlayerTransform = playerTransform;
        if (targetPlayerTransform == null && Camera.main != null)
            targetPlayerTransform = Camera.main.transform.root;

        if (targetPlayerTransform == null)
        {
            Debug.LogWarning("[ResetPosition] Player transform is not assigned and no main camera was found.");
            return;
        }

        if (positionAnchor == null)
        {
            Debug.LogWarning("[ResetPosition] Position anchor is not assigned.");
            return;
        }

        Vector3 currentPosition = targetPlayerTransform.position;
        Vector3 anchorPosition = positionAnchor.position;
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

    private void StartResetRoutine(float delay, float retryDuration)
    {
        if (resetRoutine != null)
            StopCoroutine(resetRoutine);

        resetRoutine = StartCoroutine(ResetPositionAfterDelay(delay, retryDuration));
    }

    private IEnumerator ResetPositionAfterDelay(float delay, float retryDuration)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        else
            yield return null;

        float endTime = Time.unscaledTime + Mathf.Max(0f, retryDuration);
        do
        {
            ResetPlayerPosition();
            yield return null;
        }
        while (Time.unscaledTime < endTime);

        resetRoutine = null;
    }

    private void RegisterTrackingOriginCallbacks()
    {
        UnregisterTrackingOriginCallbacks();

        SubsystemManager.GetSubsystems(inputSubsystems);
        foreach (XRInputSubsystem inputSubsystem in inputSubsystems)
        {
            if (inputSubsystem != null)
                inputSubsystem.trackingOriginUpdated += OnTrackingOriginUpdated;
        }
    }

    private void UnregisterTrackingOriginCallbacks()
    {
        foreach (XRInputSubsystem inputSubsystem in inputSubsystems)
        {
            if (inputSubsystem != null)
                inputSubsystem.trackingOriginUpdated -= OnTrackingOriginUpdated;
        }

        inputSubsystems.Clear();
    }

    private void OnTrackingOriginUpdated(XRInputSubsystem inputSubsystem)
    {
        if (!resetAfterTrackingOriginChange)
            return;

        StartResetRoutine(trackingOriginResetDelay, trackingOriginRetryDuration);
    }
}
