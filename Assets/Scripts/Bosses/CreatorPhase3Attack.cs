using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatorPhase3Attack : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private int projectileDamage = 1;

    [Header("Pattern Timing")]
    [SerializeField] private float delayBetweenPatterns = 1f;

    [Header("Spiral Attack")]
    [SerializeField] private float spiralFireRate = 0.06f;
    [SerializeField] private int spiralBulletsPerRotation = 36;
    [SerializeField] private int minSpiralArms = 3;
    [SerializeField] private int maxSpiralArms = 6;
    [SerializeField] private float spiralDuration = 6f;

    [Header("Burst Attack")]
    [SerializeField] private int burstBulletCount = 20;
    [SerializeField] private int minBurstWaves = 4;
    [SerializeField] private int maxBurstWaves = 6;
    [SerializeField] private float burstWaveDelay = 0.5f;

    [Header("Aimed Attack")]
    [SerializeField] private int aimedShotCount = 27;
    [SerializeField] private float aimedShotDelay = 0.2f;
    [SerializeField] private float aimedSpreadAngle = 12f;

    [Header("Oracle Attack (Predictive)")]
    [SerializeField] private int oracleShotCount = 5;
    [SerializeField] private float oracleShotDelay = 0.4f;
    [SerializeField] private float oracleProjectileSpeed = 10f;

    [Header("Laser Attack")]
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private float laserChargeTime = 1f;
    [SerializeField] private float laserDuration = 3f;
    [SerializeField] private float laserDamageInterval = 0.5f;
    [SerializeField] private float laserRange = 20f;
    [SerializeField] private float laserDamage = 1f;
    [SerializeField] private float laserTurnSpeedDegPerSec = 180f;
    [SerializeField] private LayerMask laserHitMask = ~0;

    [Header("Teleport Movement")]
    [SerializeField] private GridGenerator gridGenerator;
    [SerializeField] private float teleportInterval = 2.5f;
    [SerializeField] private float minDistanceFromPlayer = 3f;
    [SerializeField] private GameObject teleportVfxPrefab;

    private Transform player;
    private Rigidbody2D playerRb;
    private Transform bossTransform;
    private Rigidbody2D bossRb;

    private GameObject activeLaser;
    private List<Node> cachedValidNodes;

    private void OnEnable()
    {
        // Guard: kill any still-running state from a previous enable before starting fresh.
        StopAllCoroutines();
        if (activeLaser != null)
        {
            Destroy(activeLaser);
            activeLaser = null;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerRb = playerObj.GetComponent<Rigidbody2D>();
        }

        if (gridGenerator == null)
            gridGenerator = FindFirstObjectByType<GridGenerator>();

        bossTransform = transform.parent != null ? transform.parent : transform;
        bossRb = bossTransform.GetComponent<Rigidbody2D>();
        cachedValidNodes = null;

        // Phase 3: boss can no longer move, only teleport.
        AgentMover mover = GetComponentInParent<AgentMover>();
        if (mover != null)
        {
            mover.StopMovement();
            mover.canMove = false;
        }

        StartCoroutine(TeleportLoop());
        StartCoroutine(AttackLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (activeLaser != null)
        {
            Destroy(activeLaser);
            activeLaser = null;
        }
    }

    // ── Teleport Movement ────────────────────────────────────────────────

    private IEnumerator TeleportLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(teleportInterval);
            TeleportToRandomGridPosition();
        }
    }

    private void TeleportToRandomGridPosition()
    {
        if (gridGenerator == null || bossTransform == null) return;

        if (cachedValidNodes == null || cachedValidNodes.Count == 0)
        {
            CacheValidNodes();
            if (cachedValidNodes.Count == 0) return;
        }

        float minDistSqr = minDistanceFromPlayer * minDistanceFromPlayer;
        Node pick = null;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            Node candidate = cachedValidNodes[Random.Range(0, cachedValidNodes.Count)];
            if (candidate == null || candidate.isObstacle) continue;

            Vector3 pos = candidate.transform.position;

            if (player != null &&
                ((Vector2)pos - (Vector2)player.position).sqrMagnitude < minDistSqr)
                continue;

            if (Vector2.Distance(pos, bossTransform.position) < 0.05f) continue;

            pick = candidate;
            break;
        }

        if (pick == null) return;

        if (teleportVfxPrefab != null)
            Instantiate(teleportVfxPrefab, bossTransform.position, Quaternion.identity);

        // Set via Rigidbody2D so physics doesn't fight the position change.
        if (bossRb != null)
            bossRb.position = pick.transform.position;
        else
            bossTransform.position = pick.transform.position;

        if (teleportVfxPrefab != null)
            Instantiate(teleportVfxPrefab, bossTransform.position, Quaternion.identity);
    }

    private void CacheValidNodes()
    {
        cachedValidNodes = new List<Node>();
        Node[] all = gridGenerator.GetComponentsInChildren<Node>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && !all[i].isObstacle)
                cachedValidNodes.Add(all[i]);
        }
    }

    // ── Attack loop ──────────────────────────────────────────────────────

    private IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            int pattern = Random.Range(0, 5);
            switch (pattern)
            {
                case 0: yield return StartCoroutine(SpiralAttack()); break;
                case 1: yield return StartCoroutine(BurstAttack());  break;
                case 2: yield return StartCoroutine(AimedAttack());  break;
                case 3: yield return StartCoroutine(OracleAttack()); break;
                case 4: yield return StartCoroutine(LaserAttack());  break;
            }

            yield return new WaitForSeconds(delayBetweenPatterns);
        }
    }

    // ── Attack patterns ──────────────────────────────────────────────────

    private IEnumerator SpiralAttack()
    {
        float angleStep = 360f / spiralBulletsPerRotation;
        float currentAngle = 0f;
        int totalShots = Mathf.RoundToInt(spiralDuration / spiralFireRate);
        int spiralArms = Random.Range(minSpiralArms, maxSpiralArms + 1);

        for (int i = 0; i < totalShots; i++)
        {
            for (int arm = 0; arm < spiralArms; arm++)
            {
                float angle = currentAngle + (360f / spiralArms) * arm;
                SpawnProjectile(AngleToDirection(angle), projectileSpeed);
            }

            currentAngle += angleStep;
            yield return new WaitForSeconds(spiralFireRate);
        }
    }

    private IEnumerator BurstAttack()
    {
        float angleOffset = 0f;
        int burstWaves = Random.Range(minBurstWaves, maxBurstWaves + 1);

        for (int wave = 0; wave < burstWaves; wave++)
        {
            for (int i = 0; i < burstBulletCount; i++)
            {
                float angle = angleOffset + (360f / burstBulletCount) * i;
                SpawnProjectile(AngleToDirection(angle), projectileSpeed);
            }

            angleOffset += (360f / burstBulletCount) * 0.5f;
            yield return new WaitForSeconds(burstWaveDelay);
        }
    }

    private IEnumerator AimedAttack()
    {
        for (int i = 0; i < aimedShotCount; i++)
        {
            if (player != null)
            {
                Vector2 dirToPlayer = ((Vector2)(player.position - bossTransform.position)).normalized;
                float baseAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
                float spread = Random.Range(-aimedSpreadAngle, aimedSpreadAngle);
                SpawnProjectile(AngleToDirection(baseAngle + spread), projectileSpeed);
            }

            yield return new WaitForSeconds(aimedShotDelay);
        }
    }

    private IEnumerator OracleAttack()
    {
        for (int i = 0; i < oracleShotCount; i++)
        {
            if (player != null)
                SpawnProjectile(GetPredictiveDirection(), oracleProjectileSpeed);

            yield return new WaitForSeconds(oracleShotDelay);
        }
    }

    // ── Laser Attack (charge + tracking beam) ────────────────────────────

    private IEnumerator LaserAttack()
    {
        if (laserPrefab == null || player == null) yield break;

        GameObject laser = Instantiate(laserPrefab, bossTransform.position, Quaternion.identity);
        activeLaser = laser;

        try
        {
            Vector2 dir = ((Vector2)(player.position - bossTransform.position)).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            laser.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // ── Charge phase: thin telegraph while locking on.
            Vector3 baseScale = laser.transform.localScale;
            laser.transform.localScale = new Vector3(baseScale.x, baseScale.y * 0.25f, baseScale.z);

            float chargeElapsed = 0f;
            while (chargeElapsed < laserChargeTime)
            {
                if (laser == null || bossTransform == null) yield break;
                laser.transform.position = bossTransform.position;
                if (player != null)
                {
                    Vector2 desired = ((Vector2)(player.position - bossTransform.position)).normalized;
                    float desiredAngle = Mathf.Atan2(desired.y, desired.x) * Mathf.Rad2Deg;
                    angle = Mathf.MoveTowardsAngle(angle, desiredAngle, laserTurnSpeedDegPerSec * Time.deltaTime);
                    laser.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
                chargeElapsed += Time.deltaTime;
                yield return null;
            }

            if (laser != null) laser.transform.localScale = baseScale;

            // ── Fire phase: beam tracks player, raycast damage.
            float fireElapsed = 0f;
            float damageTimer = 0f;
            while (fireElapsed < laserDuration)
            {
                if (laser == null || bossTransform == null) yield break;
                laser.transform.position = bossTransform.position;
                if (player != null)
                {
                    Vector2 desired = ((Vector2)(player.position - bossTransform.position)).normalized;
                    float desiredAngle = Mathf.Atan2(desired.y, desired.x) * Mathf.Rad2Deg;
                    angle = Mathf.MoveTowardsAngle(angle, desiredAngle, laserTurnSpeedDegPerSec * Time.deltaTime);
                    laser.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }

                Vector2 fireDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

                damageTimer += Time.deltaTime;
                if (damageTimer >= laserDamageInterval)
                {
                    damageTimer = 0f;
                    ApplyLaserDamage(fireDir);
                }

                fireElapsed += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            if (laser != null) Destroy(laser);
            activeLaser = null;
        }
    }

    private void ApplyLaserDamage(Vector2 direction)
    {
        if (bossTransform == null) return;

        RaycastHit2D[] hits = Physics2D.RaycastAll(bossTransform.position, direction, laserRange, laserHitMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i].collider;
            if (col == null) continue;
            if (!col.CompareTag("Player")) continue;

            PlayerHealth ph = col.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(laserDamage);
            return;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private Vector2 GetPredictiveDirection()
    {
        Vector2 targetPos = player.position;

        if (playerRb != null)
        {
            float distance = Vector2.Distance(bossTransform.position, player.position);
            float travelTime = distance / oracleProjectileSpeed;
            targetPos += playerRb.linearVelocity * travelTime;
        }

        return (targetPos - (Vector2)bossTransform.position).normalized;
    }

    private void SpawnProjectile(Vector2 direction, float speed)
    {
        if (projectilePrefab == null || PoolManager.Instance == null) return;

        GameObject bullet = PoolManager.Instance.Spawn(projectilePrefab, bossTransform.position, Quaternion.identity);
        EnemyProjectile enemyProjectile = bullet.GetComponent<EnemyProjectile>();

        if (enemyProjectile != null)
        {
            enemyProjectile.damage = projectileDamage;
            enemyProjectile.Initialize(projectilePrefab);
            enemyProjectile.SetDirection(direction, speed);
        }
    }

    private Vector2 AngleToDirection(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}
