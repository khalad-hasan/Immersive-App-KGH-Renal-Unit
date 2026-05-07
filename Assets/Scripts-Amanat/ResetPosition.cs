using System.Collections;
using UnityEngine;

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

    private void Start()
    {
        if (resetPositionOnStart)
            StartCoroutine(ResetPositionAfterDelay());
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

    private IEnumerator ResetPositionAfterDelay()
    {
        if (startupDelay > 0f)
            yield return new WaitForSeconds(startupDelay);
        else
            yield return null;

        float endTime = Time.unscaledTime + Mathf.Max(0f, startupRetryDuration);
        do
        {
            ResetPlayerPosition();
            yield return null;
        }
        while (Time.unscaledTime < endTime);
    }
}
