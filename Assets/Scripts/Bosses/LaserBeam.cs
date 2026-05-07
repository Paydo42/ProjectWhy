using UnityEngine;
[DisallowMultipleComponent]

public class LaserBeam : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private float damageInterval = 0.5f;
    [SerializeField] private float range = 20f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Tracking")]
    [Tooltip("If true, the beam rotates toward its target each frame while alive.")]
    [SerializeField] private bool trackTarget = true;
    [SerializeField] private float turnSpeedDegPerSec = 180f;

    [Header("Visual Stretch")]
    [Tooltip("Child transform stretched along Y to match how far the beam reaches. Leave null to disable.")]
    [SerializeField] private Transform stretchTransform;
    [Tooltip("Unscaled length of the visual at stretchTransform.localScale.y = 1. Set this to your sprite's authored length in world units.")]
    [SerializeField] private float baseLength = 1f;

    private Transform targetTransform;
    private bool hitboxActive;
    private float damageTimer;

    public void SetTarget(Transform target) { targetTransform = target; }

    // Animation event — call on the frame the laser becomes deadly.
    public void EnableHitbox()
    {
        hitboxActive = true;
        damageTimer = damageInterval; // damage on the very first frame
    }

    // Animation event — call on the frame the laser stops being deadly.
    public void DisableHitbox() { hitboxActive = false; }

    // Animation event — call on the last frame to clean up.
    public void Despawn()
    {
        hitboxActive = false;
        Destroy(gameObject);
    }

    private void Update()
    {
        if (trackTarget && targetTransform != null) UpdateAim();
        UpdateLength();

        if (!hitboxActive) return;

        damageTimer += Time.deltaTime;
        if (damageTimer >= damageInterval)
        {
            damageTimer = 0f;
            CastForPlayer();
        }
    }

    private void UpdateLength()
    {
        if (stretchTransform == null) return;

        // Stretch only to walls — skip the player so the beam visually passes through them.
        float reach = range;
        bool hitWall = false;
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, transform.up, range, hitMask);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null) continue;
            if (hits[i].collider.CompareTag("Player")) continue;
            reach = hits[i].distance;
            hitWall = true;
            break;
        }
        if (reach < baseLength) reach = baseLength;

        // Scene-view diagnostic: green = hit a wall; red = stretched to full range.
        Debug.DrawRay(transform.position, transform.up * reach, hitWall ? Color.green : Color.red);

        Vector3 s = stretchTransform.localScale;
        s.y = reach / Mathf.Max(0.0001f, baseLength);
        stretchTransform.localScale = s;
    }

    private void UpdateAim()
    {
        Vector2 toTarget = (Vector2)(targetTransform.position - transform.position);
        if (toTarget.sqrMagnitude < 0.0001f) return;

        // -90° because the prefab's "forward" is +Y, not +X.
        float desired = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
        float current = transform.eulerAngles.z;
        float next = Mathf.MoveTowardsAngle(current, desired, turnSpeedDegPerSec * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, next);
    }

    private void CastForPlayer()
    {
        Vector2 dir = transform.up;
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, dir, range, hitMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i].collider;
            if (col == null) continue;
            if (!col.CompareTag("Player")) continue;

            PlayerHealth ph = col.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage, transform.position);
            return;
        }
    }
}
