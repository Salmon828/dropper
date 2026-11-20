using UnityEngine;

// Keeps the whole 16:9 play area visible on any screen shape.
// Narrower screens (16:10, 4:3) zoom out so the side walls aren't cut off; wider screens are left alone.
[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    [SerializeField] private float targetAspect = 16f / 9f;
    [SerializeField] private float sizeAt16x9 = 11f; // The camera's orthographic size, designed for 16:9

    private Camera cam;
    private float lastAspect;

    void Awake() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        if (Mathf.Approximately(cam.aspect, lastAspect)) return; // Only recalculate when the window shape changes
        lastAspect = cam.aspect;
        cam.orthographicSize = cam.aspect < targetAspect
            ? sizeAt16x9 * targetAspect / cam.aspect
            : sizeAt16x9;
    }
}
