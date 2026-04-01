using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChargerAttack", menuName = "Enemy Logic/Attack Logic/Charger Attack")]
public class ChargerAttackBehavior : EnemyAttackSOBase
{
    [Header("Charge Settings")]
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float normalSpeed = 3f;
    [SerializeField] private float arrivalThreshold = 0.5f;

    [Header("Ray Scan Settings")]
    [SerializeField] private float rayDistance = 15f;
    [SerializeField] private float cardinalChargeDistance = 10f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask playerLayer;

    [Header("Timing")]
    [SerializeField] private float scanPauseDuration = 0.4f;
    [SerializeField] private float postChargeDelay = 0.2f;

    private enum ChargerState { ChargingToTarget, Scanning, ChargingCardinal }

    private ChargerState currentState;
    private Vector3 chargeTarget;
    private float scanTimer;
    private float originalSpeed;
    private bool hasStoredSpeed;

    private static readonly Vector2[] cardinalDirs =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        // Store and override speed
        if (enemy.agentMover != null && !hasStoredSpeed)
        {
            originalSpeed = enemy.agentMover.moveSpeed;
            hasStoredSpeed = true;
        }

        // Record player's current position as the charge target
        chargeTarget = playerTransform.position;
        StartChargeTo(chargeTarget);
    }

    public override void DoExitLogic()
    {
        // Restore original speed
        if (enemy.agentMover != null && hasStoredSpeed)
        {
            enemy.agentMover.moveSpeed = originalSpeed;
        }
        enemy.StopPathfinding();
        base.DoExitLogic();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        switch (currentState)
        {
            case ChargerState.ChargingToTarget:
            case ChargerState.ChargingCardinal:
                UpdateCharging();
                break;

            case ChargerState.Scanning:
                UpdateScanning();
                break;
        }
    }

    // ── Charging ────────────────────────────────────────────────────────

    private void StartChargeTo(Vector3 target)
    {
        chargeTarget = target;
        currentState = ChargerState.ChargingToTarget;

        if (enemy.agentMover != null)
            enemy.agentMover.moveSpeed = chargeSpeed;

        enemy.RequestPath(chargeTarget);
        if (enemy.agentMover != null)
            enemy.agentMover.canMove = true;
    }

    private void StartCardinalCharge(Vector2 direction)
    {
        // Target is far in the cardinal direction; walls/grid will stop it naturally
        Vector3 target = enemy.transform.position + (Vector3)(direction * cardinalChargeDistance);

        // Snap to nearest valid grid node so pathfinding works
        if (enemy.currentRoomGridGenerator != null)
        {
            Node node = enemy.currentRoomGridGenerator.GetNodeFromWorldPoint(target);
            if (node != null && !node.isObstacle)
                target = node.transform.position;
            else
                target = FindFarthestValidNodeInDirection(direction);
        }

        chargeTarget = target;
        currentState = ChargerState.ChargingCardinal;

        if (enemy.agentMover != null)
            enemy.agentMover.moveSpeed = chargeSpeed;

        enemy.RequestPath(chargeTarget);
        if (enemy.agentMover != null)
            enemy.agentMover.canMove = true;
    }

    private void UpdateCharging()
    {
        bool arrived = false;

        // Check if AgentMover finished its path
        if (enemy.agentMover != null && !enemy.agentMover.isFollowingPath)
            arrived = true;

        // Also check distance as fallback
        float dist = Vector2.Distance(enemy.transform.position, chargeTarget);
        if (dist <= arrivalThreshold)
            arrived = true;

        if (arrived)
        {
            enemy.StopPathfinding();
            if (enemy.agentMover != null)
                enemy.agentMover.moveSpeed = normalSpeed;

            // Enter scanning state
            currentState = ChargerState.Scanning;
            scanTimer = 0f;
        }
    }

    // ── Scanning ────────────────────────────────────────────────────────

    private void UpdateScanning()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer < scanPauseDuration) return;

        // Shoot 4 cardinal rays to find the player
        Vector2 hitDirection = Vector2.zero;
        bool foundPlayer = false;

        foreach (Vector2 dir in cardinalDirs)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                enemy.transform.position, dir, rayDistance, playerLayer | obstacleLayer);

            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                hitDirection = dir;
                foundPlayer = true;
                break;
            }
        }

        if (foundPlayer)
        {
            // Ray found the player — charge in that cardinal direction
            StartCardinalCharge(hitDirection);
        }
        else
        {
            // No ray hit — pick the cardinal direction closest to the player
            Vector2 toPlayer = (Vector2)(playerTransform.position - enemy.transform.position);
            Vector2 bestDir = GetClosestCardinalDirection(toPlayer);
            StartCardinalCharge(bestDir);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private Vector2 GetClosestCardinalDirection(Vector2 toPlayer)
    {
        Vector2 best = Vector2.right;
        float bestDot = float.MinValue;

        foreach (Vector2 dir in cardinalDirs)
        {
            float dot = Vector2.Dot(toPlayer.normalized, dir);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = dir;
            }
        }

        return best;
    }

    private Vector3 FindFarthestValidNodeInDirection(Vector2 direction)
    {
        // Walk along the cardinal direction and find the last valid node
        GridGenerator grid = enemy.currentRoomGridGenerator;
        if (grid == null) return enemy.transform.position;

        Vector3 bestPos = enemy.transform.position;
        float step = 1f;

        for (float d = step; d <= cardinalChargeDistance; d += step)
        {
            Vector3 testPos = enemy.transform.position + (Vector3)(direction * d);
            Node node = grid.GetNodeFromWorldPoint(testPos);

            if (node == null || node.isObstacle)
                break;

            bestPos = node.transform.position;
        }

        return bestPos;
    }

    public override void ResetValues()
    {
        base.ResetValues();
        currentState = ChargerState.ChargingToTarget;
        scanTimer = 0f;
    }
}
