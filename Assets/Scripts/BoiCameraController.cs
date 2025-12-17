using UnityEngine;
using UnityEngine.U2D; // Required for PixelPerfectCamera check if needed

public class BoiCameraController : MonoBehaviour
{
    public Transform target;
    [Range(0.01f, 5f)]

    public float smoothTime = 0.15f;
    public Vector2 centerOffset = new Vector2(0, 1f); // Adjust Y to see more of the top wall
    
    private Rect activeBounds;
    private Camera cam;
    private float vertExtent, horzExtent;
    private Vector3 currentVelocity = Vector3.zero;

    void Start()
    {
        cam = Camera.main;
        // Don't calculate extents here; PixelPerfectCamera might change them later.
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Pixel Perfect kamera boyutu değiştirebileceği için her karede sınırları güncelle
        vertExtent = cam.orthographicSize;
        horzExtent = vertExtent * cam.aspect;

        // 1. Hedef Pozisyonu Belirle (Ama henüz atama!)
        Vector3 targetPos = new Vector3(target.position.x + centerOffset.x, target.position.y + centerOffset.y, transform.position.z);

        // Odanın sınırlarını hesapla
        float minX = activeBounds.xMin + horzExtent;
        float maxX = activeBounds.xMax - horzExtent;
        float minY = activeBounds.yMin + vertExtent;
        float maxY = activeBounds.yMax - vertExtent;

        // 2. Akıllı Clamp (Oda küçükse ortala, büyükse sınırla)
        // Yatay Kontrol
        if (maxX < minX)
            targetPos.x = activeBounds.center.x;
        else
            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);

        // Dikey Kontrol
        if (maxY < minY)
            targetPos.y = activeBounds.center.y;
        else
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        // 3. SMOOTH DAMP (Küt diye durmayı engelleyen kısım)
        // Kamerayı şu anki pozisyonundan, hesaplanan 'targetPos'a doğru yumuşakça kaydırır.
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);
    }
    public void SetActiveBounds(Rect bounds)
    {
        activeBounds = bounds;
    }
}