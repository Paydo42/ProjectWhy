using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomBounds : MonoBehaviour
{
    private BoxCollider2D box;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;  // make sure the collider is a trigger
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // take bounds from the box collider
        Rect bounds = new Rect(
            box.bounds.min.x,
            box.bounds.min.y,
            box.bounds.size.x,
            box.bounds.size.y
        );

        
        var camCtrl = Camera.main.GetComponent<BoiCameraController>();
        if (camCtrl != null)
            camCtrl.SetActiveBounds(bounds);
    }
}