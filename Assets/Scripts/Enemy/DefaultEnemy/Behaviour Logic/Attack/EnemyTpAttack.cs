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

    [Header("Attack Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private float chargeTime = 1f;
    [SerializeField] private float spreadAngle = 4f;

    private enum TpState { Teleporting, Charging, Shooting, Cooldown }
    private TpState currentState;
    private float stateTimer;

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

        currentState = TpState.Teleporting;
        stateTimer = 0f;
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        switch (currentState)
        {
            case TpState.Teleporting:
                TeleportToRandomPosition();
                currentState = TpState.Charging;
                stateTimer = 0f;
                break;

            case TpState.Charging:
                stateTimer += Time.deltaTime;
                if (stateTimer >= chargeTime)
                {
                    currentState = TpState.Shooting;
                }
                break;

            case TpState.Shooting:
                ShootAtPlayer();
                currentState = TpState.Cooldown;
                stateTimer = 0f;
                break;

            case TpState.Cooldown:
                stateTimer += Time.deltaTime;
                if (stateTimer >= teleportCooldown)
                {
                    currentState = TpState.Teleporting;
                }
                break;
        }
    }

    private void TeleportToRandomPosition()
    {
        if (gridGenerator == null) return;

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

            if (playerTransform != null &&
                ((Vector2)pos - (Vector2)playerTransform.position).sqrMagnitude < minDistSqr)
                continue;

            if (Vector2.Distance(pos, transform.position) < 0.05f) continue;

            pick = candidate;
            break;
        }

        if (pick == null) return;

        if (teleportVfxPrefab != null)
            Object.Instantiate(teleportVfxPrefab, transform.position, Quaternion.identity);

        if (enemyRb != null)
            enemyRb.position = pick.transform.position;
        else
            transform.position = pick.transform.position;

        if (teleportVfxPrefab != null)
            Object.Instantiate(teleportVfxPrefab, transform.position, Quaternion.identity);
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

    public override void ResetValues()
    {
        base.ResetValues();
        currentState = TpState.Teleporting;
        stateTimer = 0f;
        cachedValidNodes = null;
    }
}
