using UnityEngine;

[CreateAssetMenu(fileName = "ChargerAttack", menuName = "Enemy Logic/Attack Logic/Charger Attack")]
public class ChargerAttackBehavior : EnemyAttackSOBase
{
    public override bool ManagesOwnTransitions => true;

    [Header("Charge Settings")]
    [SerializeField] private float chargeSpeed = 10f;
    [SerializeField] private float approachSpeed = 3f;
    [SerializeField] private float wallCheckDistance = 0.7f;
    [SerializeField] private float arrivalDistance = 0.75f;

    [Header("Approach Redirect (unpredictability)")]
    [SerializeField] private float minRedirectInterval = 0.4f;
    [SerializeField] private float maxRedirectInterval = 1.2f;

    [Header("Scan Settings")]
    [SerializeField] private float scanPauseDuration = 0.3f;
    [SerializeField] private float rayDistance = 15f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask playerLayer;

    // Charging   = fast cardinal dash until wall
    // Scanning   = brief pause + 3 rays (forward, left, right — not behind)
    // Approaching = normal speed cardinal walk toward player
    private enum ChargerState { Charging, Scanning, Approaching }

    private ChargerState currentState;
    private Vector2 chargeDirection;
    private float scanTimer;
    private float redirectTimer;
    private float nextRedirectTime;

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        if (enemy.cardinalMover != null)
            enemy.cardinalMover.canMove = true;

        chargeDirection = CardinalToward(playerTransform.position);
        BeginCharge(chargeDirection);
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        if (enemy.cardinalMover != null)
            enemy.cardinalMover.StopMovement();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        switch (currentState)
        {
            case ChargerState.Charging:
                UpdateCharging();
                break;
            case ChargerState.Approaching:
                UpdateApproaching();
                break;
            case ChargerState.Scanning:
                UpdateScanning();
                break;
        }
    }

    // ── Charge ──────────────────────────────────────────────────────────

    private void BeginCharge(Vector2 cardinalDir)
    {
        currentState = ChargerState.Charging;
        chargeDirection = cardinalDir;

        if (enemy.cardinalMover != null)
        {
            enemy.cardinalMover.moveSpeed = chargeSpeed;
            enemy.cardinalMover.SetDirection(cardinalDir);
            enemy.cardinalMover.canMove = true;
        }
    }

    private void UpdateCharging()
    {
        // Stop when the charge direction hits a wall
        if (IsWallInDirection(chargeDirection))
        {
            if (enemy.cardinalMover != null) enemy.cardinalMover.StopMovement();
            currentState = ChargerState.Scanning;
            scanTimer = 0f;
        }
    }

    // ── Approach ────────────────────────────────────────────────────────

    private void BeginApproach(Vector2 cardinalDir)
    {
        currentState = ChargerState.Approaching;
        chargeDirection = cardinalDir;
        redirectTimer = 0f;
        nextRedirectTime = Random.Range(minRedirectInterval, maxRedirectInterval);

        if (enemy.cardinalMover != null)
        {
            enemy.cardinalMover.moveSpeed = approachSpeed;
            enemy.cardinalMover.SetDirection(cardinalDir);
            enemy.cardinalMover.canMove = true;
        }
    }

    private void UpdateApproaching()
    {
        Vector2 forward = chargeDirection;
        if (forward.sqrMagnitude < 0.01f) forward = Vector2.right;

        // Player directly ahead — begin new charge immediately
        if (CastRayForPlayer(forward))
        {
            BeginCharge(CardinalToward(playerTransform.position));
            return;
        }

        // Wall ahead OR close enough to player — stop and scan
        if (IsWallInDirection(chargeDirection) ||
            Vector2.Distance(enemy.transform.position, playerTransform.position) <= arrivalDistance + 1f)
        {
            if (enemy.cardinalMover != null) enemy.cardinalMover.StopMovement();
            currentState = ChargerState.Scanning;
            scanTimer = 0f;
            return;
        }

        // Random redirect — re-pick a cardinal toward the player to be unpredictable
        redirectTimer += Time.deltaTime;
        if (redirectTimer >= nextRedirectTime)
        {
            redirectTimer = 0f;
            nextRedirectTime = Random.Range(minRedirectInterval, maxRedirectInterval);

            Vector2 newDir = CardinalToward(playerTransform.position);
            if (!IsWallInDirection(newDir))
            {
                chargeDirection = newDir;
                if (enemy.cardinalMover != null)
                    enemy.cardinalMover.SetDirection(newDir);
            }
        }
    }

    // ── 3-Ray Scan (forward, left, right — NOT behind) ──────────────────

    private void UpdateScanning()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer < scanPauseDuration) return;

        Vector2 forward = chargeDirection;
        if (forward.sqrMagnitude < 0.01f) forward = Vector2.right;

        Vector2 left  = new Vector2(-forward.y, forward.x);   // 90° left
        Vector2 right = new Vector2(forward.y, -forward.x);   // 90° right

        if (CastRayForPlayer(forward) || CastRayForPlayer(left) || CastRayForPlayer(right))
        {
            // Hit player — charge in cardinal direction toward them
            BeginCharge(CardinalToward(playerTransform.position));
        }
        else
        {
            // No hit — approach player at normal speed, scan again on arrival
            BeginApproach(CardinalToward(playerTransform.position));
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private bool IsWallInDirection(Vector2 dir)
    {
        Vector2 origin = (Vector2)enemy.transform.position + dir * 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, wallCheckDistance, obstacleLayer);
        return hit.collider != null;
    }

    private bool CastRayForPlayer(Vector2 direction)
    {
        Vector2 origin = (Vector2)enemy.transform.position + direction * 0.6f;
        RaycastHit2D hit = Physics2D.Raycast(
            origin, direction, rayDistance, playerLayer | obstacleLayer);
        Debug.DrawRay(origin, direction * rayDistance, Color.yellow);
        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    private Vector2 CardinalToward(Vector2 target)
    {
        Vector2 dir = (target - (Vector2)enemy.transform.position);
        if (dir.sqrMagnitude < 0.0001f) return Vector2.right;
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            return dir.x >= 0 ? Vector2.right : Vector2.left;
        else
            return dir.y >= 0 ? Vector2.up : Vector2.down;
    }

    public override void ResetValues()
    {
        base.ResetValues();
        currentState = ChargerState.Charging;
        scanTimer = 0f;
    }
}
