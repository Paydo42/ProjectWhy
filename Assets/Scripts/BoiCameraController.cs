using UnityEngine;

public class BoiCameraController : MonoBehaviour
{
    public Transform target;
    private Rect activeBounds;
    private Camera cam;
    private float vertExtent, horzExtent;

    void Start()
    {
        cam = Camera.main;
        vertExtent = cam.orthographicSize;
        horzExtent = vertExtent * cam.aspect;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);

        // Clamp the camera position to the active bounds
        float minX = activeBounds.xMin + horzExtent;
        float maxX = activeBounds.xMax - horzExtent;
        float minY = activeBounds.yMin + vertExtent;
        float maxY = activeBounds.yMax - vertExtent;

        float clampedX = Mathf.Clamp(desired.x, minX, maxX);
        float clampedY = Mathf.Clamp(desired.y, minY, maxY);

        transform.position = new Vector3(clampedX, clampedY, desired.z);
    }

    public void SetActiveBounds(Rect bounds)
    {
        activeBounds = bounds;
    }
}
