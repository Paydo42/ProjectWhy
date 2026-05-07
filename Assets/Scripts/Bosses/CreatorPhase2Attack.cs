using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatorPhase2Attack : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private int projectileDamage = 1;

    [Header("Spiral Attack")]
    [SerializeField] private float spiralFireRate = 0.06f;
    [SerializeField] private int spiralBulletsPerRotation = 36;
    [SerializeField] private int minSprialArms = 2;
     [SerializeField] private int maxSpiralArms = 5;
    [SerializeField] private float spiralDuration = 6f;

    [Header("Burst Attack")]
    [SerializeField] private int burstBulletCount = 20;
    [SerializeField] private int minBurstWaves = 3;
    [SerializeField] private int maxBurstWaves = 5;
    [SerializeField] private float burstWaveDelay = 0.5f;

    [Header("Aimed Attack")]
    [SerializeField] private int aimedShotCount = 27;
    [SerializeField] private float aimedShotDelay = 0.2f;
    [SerializeField] private float aimedSpreadAngle = 12f;

    [Header("Oracle Attack (Predictive)")]
    [SerializeField] private int oracleShotCount = 5;
    [SerializeField] private float oracleShotDelay = 0.4f;
    [SerializeField] private float oracleProjectileSpeed = 10f;

    [Header("Grid Movement")]
    [SerializeField] private GridGenerator gridGenerator;
    [SerializeField] private float pathUpdateInterval = 2f;
    [SerializeField] private float wanderRadius = 5f;
    [Tooltip("Wall/obstacle layer used to validate that A* paths don't physically cross walls. Set this to match the chamber's wall layer.")]
    [SerializeField] private LayerMask wallLayer;

    [Header("Pattern Timing")]
    [SerializeField] private float delayBetweenPatterns = 1f;

    private Transform player;
    private Rigidbody2D playerRb;
    private AgentMover agentMover;
    private Transform bossTransform;
    private Vector2 spawnPosition;

    private Coroutine attackRoutine;
    private Coroutine movementRoutine;

    // Valid (non-obstacle) nodes within wanderRadius of spawn, cached on first use.
    private List<Node> cachedWanderNodes;

    private void OnEnable()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerRb = playerObj.GetComponent<Rigidbody2D>();
        }

        agentMover = GetComponentInParent<AgentMover>();

        if (gridGenerator == null)
            gridGenerator = FindFirstObjectByType<GridGenerator>();

        bossTransform = transform.parent != null ? transform.parent : transform;
        spawnPosition = bossTransform.position;
        cachedWanderNodes = null;

        // Movement and attacks run in parallel
        movementRoutine = StartCoroutine(WanderLoop());
        attackRoutine   = StartCoroutine(AttackLoop());
    }

    private void OnDisable()
    {
        if (attackRoutine != null)  { StopCoroutine(attackRoutine);  attackRoutine  = null; }
        if (movementRoutine != null){ StopCoroutine(movementRoutine); movementRoutine = null; }

        if (agentMover != null)
            agentMover.StopMovement();
    }

    // ── Continuous grid-based wandering ──────────────────────────────────

    private IEnumerator WanderLoop()
    {
        while (true)
        {
            MoveToRandomGridPosition();
            yield return new WaitForSeconds(pathUpdateInterval);
        }
    }

    private void MoveToRandomGridPosition()
    {
        if (agentMover == null || gridGenerator == null || AStarManager.Instance == null) return;

        if (cachedWanderNodes == null) CacheWanderNodes();
        if (cachedWanderNodes.Count == 0)
        {
            Debug.LogWarning($"[{name}] Phase2 wander: no valid grid nodes found within {wanderRadius} units of spawn.");
            return;
        }

        Transform bossTf = transform.parent != null ? transform.parent : transform;

        // Pick a node, retry if FindPath happens to fail or the node coincides with current pos.
        for (int attempt = 0; attempt < 5; attempt++)
        {
            Node targetNode = cachedWanderNodes[Random.Range(0, cachedWanderNodes.Count)];
            if (targetNode == null) continue;
            if (Vector2.Distance(bossTf.position, targetNode.transform.position) < 0.05f) continue;

            List<Node> path = AStarManager.Instance.FindPath(gridGenerator, bossTf.position, targetNode.transform.position);
            if (path != null && path.Count > 0)
            {
                agentMover.canMove = true;
                agentMover.SetPath(path);
                return;
            }
        }

        Debug.LogWarning($"[{name}] Phase2 wander: FindPath failed for 5 candidate nodes; check the grid/agent area.");
    }

    private void CacheWanderNodes()
    {
        cachedWanderNodes = new List<Node>();
        Node[] all = gridGenerator.GetComponentsInChildren<Node>();
        float radiusSqr = wanderRadius * wanderRadius;

        for (int i = 0; i < all.Length; i++)
        {
            Node n = all[i];
            if (n == null || n.isObstacle) continue;
            if (((Vector2)n.transform.position - spawnPosition).sqrMagnitude > radiusSqr) continue;
            if (!IsNodePhysicallyReachable(n)) continue;
            cachedWanderNodes.Add(n);
        }
    }

    // True if A* finds a path AND every segment of that path is clear of walls.
    // This rejects nodes that A* can technically reach but only via a route the
    // boss can't physically traverse (e.g. corner-cutting through a wall).
    private bool IsNodePhysicallyReachable(Node target)
    {
        if (AStarManager.Instance == null) return false;

        List<Node> path = AStarManager.Instance.FindPath(gridGenerator, spawnPosition, target.transform.position);
        if (path == null || path.Count == 0) return false;

        Vector3 prev = spawnPosition;
        for (int j = 0; j < path.Count; j++)
        {
            Vector3 next = path[j].transform.position;
            if (Physics2D.Linecast(prev, next, wallLayer).collider != null)
                return false;
            prev = next;
        }
        return true;
    }

    // ── Random attack loop (independent of movement) ─────────────────────

    private IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            int pattern = Random.Range(0, 4);
            switch (pattern)
            {
                case 0: yield return StartCoroutine(SpiralAttack());  break;
                case 1: yield return StartCoroutine(BurstAttack());   break;
                case 2: yield return StartCoroutine(AimedAttack());   break;
                case 3: yield return StartCoroutine(OracleAttack());  break;
            }

            yield return new WaitForSeconds(delayBetweenPatterns);
        }
    }

    // ── Attack patterns ───────────────────────────────────────────────────

    private IEnumerator SpiralAttack()
    {
        float angleStep   = 360f / spiralBulletsPerRotation;
        float currentAngle = 0f;
        int totalShots    = Mathf.RoundToInt(spiralDuration / spiralFireRate);
        int spiralArms    = Random.Range(minSprialArms, maxSpiralArms + 1);
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
                float spread    = Random.Range(-aimedSpreadAngle, aimedSpreadAngle);
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

    private Vector2 GetPredictiveDirection()
    {
        Vector2 targetPos = player.position;

        if (playerRb != null)
        {
            float distance   = Vector2.Distance(bossTransform.position, player.position);
            float travelTime = distance / oracleProjectileSpeed;
            targetPos += playerRb.linearVelocity * travelTime;
        }

        return (targetPos - (Vector2)bossTransform.position).normalized;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void SpawnProjectile(Vector2 direction, float speed)
    {
        if (projectilePrefab == null || PoolManager.Instance == null) return;

        GameObject bullet = PoolManager.Instance.Spawn(projectilePrefab, bossTransform.position, Quaternion.identity);
        EnemyProjectile ep = bullet.GetComponent<EnemyProjectile>();

        if (ep != null)
        {
            ep.damage = projectileDamage;
            ep.Initialize(projectilePrefab);
            ep.SetDirection(direction, speed);
        }
    }

    private Vector2 AngleToDirection(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}
