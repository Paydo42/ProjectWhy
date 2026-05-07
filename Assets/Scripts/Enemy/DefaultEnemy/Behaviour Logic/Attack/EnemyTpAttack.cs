using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAttackTp", menuName = "Enemy Logic/Attack Logic/EnemyAttackTp")]
public class EnemyAttackTp : EnemyAttackSOBase
{
    public override bool ManagesOwnTransitions => true;

    [Header("Teleportation Settings")]
    [SerializeField] private float teleportCooldown = 3f;
    [SerializeField] private float minDistanceFromPlayer = 3f;
    [SerializeField] private GameObject teleportVfxPrefab;

    [Header("Animator Bool Names (leave blank to skip)")]
    [Tooltip("True while the begin-teleport animation plays at the old position. Last frame must call OnTpBeginAnimationEnd.")]
    [SerializeField] private string tpBeginBoolName  = "IsTpBegin";
    [Tooltip("True while the appear animation plays at the new position. Last frame must call OnTpAppearAnimationEnd.")]
    [SerializeField] private string tpAppearBoolName = "IsTpAppear";

    [Header("Animation Event Fallback Timeouts")]
    [Tooltip("Force-recover if the begin animation event never fires within this time. 0 disables.")]
    [SerializeField] private float tpBeginFallbackTimeout = 2f;
    [Tooltip("Force-recover if the appear animation event never fires within this time. 0 disables.")]
    [SerializeField] private float tpAppearFallbackTimeout = 2f;

    [Header("Attack Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private float chargeTime = 1f;
    [SerializeField] private float spreadAngle = 4f;

    // TpBegin   = telegraph anim at old position; ends with OnTpBeginAnimationEnd → snap + TpAppear
    // TpAppear  = arrival anim at new position; ends with OnTpAppearAnimationEnd → Charging
    // Charging  = timer-based aim/charge
    // Shooting  = fire one volley
    // Cooldown  = wait, then loop back to TpBegin
    private enum TpState { TpBegin, TpAppear, Charging, Shooting, Cooldown }
    private TpState currentState;
    private float stateTimer;
    private float phaseTimer;
    private Vector3 pendingDestination;

    private List<Node> cachedValidNodes;
    private GridGenerator gridGenerator;
    private Rigidbody2D enemyRb;

    public override void Initialize(GameObject ownerGameObject, Enemy ownerEnemy)
    {
        base.Initialize(ownerGameObject, ownerEnemy);
        enemyRb = ownerGameObject.GetComponent<Rigidbody2D>();
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        gridGenerator = enemy.currentRoomGridGenerator;
        cachedValidNodes = null;

        enemy.StopPathfinding();
        if (enemy.agentMover != null)
        {
            enemy.agentMover.StopMovement();
            enemy.agentMover.canMove = false;
        }

        BeginTpBegin();
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        SetAnimBools(false, false);
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        switch (currentState)
        {
            case TpState.TpBegin:
                UpdateTpBeginFallback();
                break;
            case TpState.TpAppear:
                UpdateTpAppearFallback();
                break;
            case TpState.Charging:
                stateTimer += Time.deltaTime;
                if (stateTimer >= chargeTime)
                    currentState = TpState.Shooting;
                break;
            case TpState.Shooting:
                ShootAtPlayer();
                currentState = TpState.Cooldown;
                stateTimer = 0f;
                break;
            case TpState.Cooldown:
                stateTimer += Time.deltaTime;
                if (stateTimer >= teleportCooldown)
                    BeginTpBegin();
                break;
        }
    }

    // ── TpBegin ─────────────────────────────────────────────────────────

    private void BeginTpBegin()
    {
        currentState = TpState.TpBegin;
        phaseTimer = 0f;

        // Lock destination now so the snap on event-end is deterministic.
        if (!TryPickTeleportDestination(out pendingDestination))
            pendingDestination = transform.position;

        SetAnimBools(begin: true, appear: false);
    }

    private void UpdateTpBeginFallback()
    {
        if (tpBeginFallbackTimeout <= 0f) return;
        phaseTimer += Time.deltaTime;
        if (phaseTimer >= tpBeginFallbackTimeout)
        {
            Debug.LogWarning($"[Sorcerer] TpBegin animation event timed out after {tpBeginFallbackTimeout}s — recovering. " +
                             "Wire OnTpBeginAnimationEnd onto the begin clip's last frame.", enemy);
            OnTpBeginAnimationEnd();
        }
    }

    // Animation event hook — fired at the last frame of the begin-tp clip.
    public void OnTpBeginAnimationEnd()
    {
        if (currentState != TpState.TpBegin) return;

        if (teleportVfxPrefab != null)
            Object.Instantiate(teleportVfxPrefab, transform.position, Quaternion.identity);

        if (enemyRb != null)
            enemyRb.position = pendingDestination;
        else
            transform.position = pendingDestination;

        if (teleportVfxPrefab != null)
            Object.Instantiate(teleportVfxPrefab, transform.position, Quaternion.identity);

        BeginTpAppear();
    }

    // ── TpAppear ────────────────────────────────────────────────────────

    private void BeginTpAppear()
    {
        currentState = TpState.TpAppear;
        phaseTimer = 0f;
        SetAnimBools(begin: false, appear: true);
    }

    private void UpdateTpAppearFallback()
    {
        if (tpAppearFallbackTimeout <= 0f) return;
        phaseTimer += Time.deltaTime;
        if (phaseTimer >= tpAppearFallbackTimeout)
        {
            Debug.LogWarning($"[Sorcerer] TpAppear animation event timed out after {tpAppearFallbackTimeout}s — recovering. " +
                             "Wire OnTpAppearAnimationEnd onto the appear clip's last frame.", enemy);
            OnTpAppearAnimationEnd();
        }
    }

    // Animation event hook — fired at the last frame of the appear clip.
    public void OnTpAppearAnimationEnd()
    {
        if (currentState != TpState.TpAppear) return;
        SetAnimBools(begin: false, appear: false);
        currentState = TpState.Charging;
        stateTimer = 0f;
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private bool TryPickTeleportDestination(out Vector3 dest)
    {
        dest = transform.position;
        if (gridGenerator == null) return false;

        if (cachedValidNodes == null || cachedValidNodes.Count == 0)
        {
            CacheValidNodes();
            if (cachedValidNodes.Count == 0) return false;
        }

        float minDistSqr = minDistanceFromPlayer * minDistanceFromPlayer;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            Node candidate = cachedValidNodes[Random.Range(0, cachedValidNodes.Count)];
            if (candidate == null || candidate.isObstacle) continue;

            Vector3 pos = candidate.transform.position;

            if (playerTransform != null &&
                ((Vector2)pos - (Vector2)playerTransform.position).sqrMagnitude < minDistSqr)
                continue;

            if (Vector2.Distance(pos, transform.position) < 0.05f) continue;

            dest = pos;
            return true;
        }

        return false;
    }

    private void ShootAtPlayer()
    {
        if (playerTransform == null || projectilePrefab == null || PoolManager.Instance == null) return;

        Vector2 dirToPlayer = ((Vector2)(playerTransform.position - transform.position)).normalized;
        float baseAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
        float spread = Random.Range(-spreadAngle, spreadAngle);
        float rad = (baseAngle + spread) * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        GameObject bullet = PoolManager.Instance.Spawn(projectilePrefab, transform.position, Quaternion.identity);
        EnemyProjectile ep = bullet.GetComponent<EnemyProjectile>();
        if (ep != null)
        {
            ep.damage = projectileDamage;
            ep.Initialize(projectilePrefab);
            ep.SetDirection(direction, projectileSpeed);
        }
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

    private void SetAnimBools(bool begin, bool appear)
    {
        if (enemy.animator == null) return;
        if (!string.IsNullOrEmpty(tpBeginBoolName))  enemy.animator.SetBool(tpBeginBoolName,  begin);
        if (!string.IsNullOrEmpty(tpAppearBoolName)) enemy.animator.SetBool(tpAppearBoolName, appear);
    }

    public override void ResetValues()
    {
        base.ResetValues();
        currentState = TpState.TpBegin;
        stateTimer = 0f;
        phaseTimer = 0f;
        cachedValidNodes = null;
    }
}
