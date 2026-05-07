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

    [Header("Animator Bool Names (leave blank to skip)")]
    [Tooltip("Driven true while telegraphing. The clip's animation event must call OnWindupAnimationEnd() to start the dash.")]
    [SerializeField] private string windupBoolName  = "IsWindup";
    [SerializeField] private string chargeBoolName  = "IsCharging";
    [Tooltip("Driven true while stuck on the wall. The clip's animation event must call OnWallStunAnimationEnd() to recover.")]
    [SerializeField] private string stunBoolName    = "IsWallStunned";

    [Header("Animation Event Fallback Timeouts")]
    [Tooltip("Safety net: if the windup animation event never fires within this many seconds, force-recover. Set to 0 to disable.")]
    [SerializeField] private float windupFallbackTimeout = 2f;
    [Tooltip("Safety net: if the wall-stun animation event never fires within this many seconds, force-recover. Set to 0 to disable.")]
    [SerializeField] private float wallStunFallbackTimeout = 2f;

    [Header("Approach Redirect (unpredictability)")]
    [SerializeField] private float minRedirectInterval = 0.4f;
    [SerializeField] private float maxRedirectInterval = 1.2f;

    [Header("Scan Settings")]
    [SerializeField] private float scanPauseDuration = 0.3f;
    [SerializeField] private float rayDistance = 15f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask playerLayer;

    // Windup     = telegraph pause before a dash
    // Charging   = fast cardinal dash until wall
    // WallStun   = stuck-on-wall recovery pause after a dash
    // Scanning   = brief pause + 3 rays (forward, left, right — not behind)
    // Approaching = normal speed cardinal walk toward player
    private enum ChargerState { Windup, Charging, WallStun, Scanning, Approaching }

    private ChargerState currentState;
    private Vector2 chargeDirection;
    private float scanTimer;
    private float redirectTimer;
    private float nextRedirectTime;
    private float phaseTimer; // Drives the windup/wall-stun animation-event fallbacks.

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        if (enemy.cardinalMover != null)
            enemy.cardinalMover.canMove = true;

        chargeDirection = CardinalToward(playerTransform.position);
        BeginWindup(chargeDirection);
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        if (enemy.cardinalMover != null)
            enemy.cardinalMover.StopMovement();
        SetAnimBools(false, false, false);
        enemy.SuppressIsWalkingAnim = false;
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        switch (currentState)
        {
            case ChargerState.Windup:
                UpdateWindupFallback();
                break;
            case ChargerState.WallStun:
                UpdateWallStunFallback();
                break;
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

    // Fires OnWindupAnimationEnd manually if the animation event doesn't arrive in time.
    private void UpdateWindupFallback()
    {
        if (windupFallbackTimeout <= 0f) return;
        phaseTimer += Time.deltaTime;
        if (phaseTimer >= windupFallbackTimeout)
        {
            Debug.LogWarning($"[Charger] Windup animation event timed out after {windupFallbackTimeout}s — recovering. " +
                             "Check that OnWindupAnimationEnd is wired on the windup clip's last frame.", enemy);
            OnWindupAnimationEnd();
        }
    }

    // Fires OnWallStunAnimationEnd manually if the animation event doesn't arrive in time.
    private void UpdateWallStunFallback()
    {
        if (wallStunFallbackTimeout <= 0f) return;
        phaseTimer += Time.deltaTime;
        if (phaseTimer >= wallStunFallbackTimeout)
        {
            Debug.LogWarning($"[Charger] Wall-stun animation event timed out after {wallStunFallbackTimeout}s — recovering. " +
                             "Check that OnWallStunAnimationEnd is wired on the wall-stun clip's last frame.", enemy);
            OnWallStunAnimationEnd();
        }
    }

    // ── Windup ──────────────────────────────────────────────────────────

    private void BeginWindup(Vector2 cardinalDir)
    {
        currentState = ChargerState.Windup;
        chargeDirection = cardinalDir;
        phaseTimer = 0f;

        if (enemy.cardinalMover != null)
            enemy.cardinalMover.StopMovement();

        // Force the blend-tree (LastMoveX/Y) to face the charge direction
        // so the telegraph animation aims at the player even though we're stationary.
        enemy.SetLastMoveDirection(cardinalDir);

        SetAnimBools(windup: true, charge: false, stun: false);
    }

    // Animation event hook — called from the windup clip's last frame.
    public void OnWindupAnimationEnd()
    {
        if (currentState != ChargerState.Windup) return;
        BeginCharge(chargeDirection);
    }

    // ── Charge ──────────────────────────────────────────────────────────

    private void BeginCharge(Vector2 cardinalDir)
    {
        currentState = ChargerState.Charging;
        chargeDirection = cardinalDir;

        SetAnimBools(windup: false, charge: true, stun: false);
        enemy.SuppressIsWalkingAnim = true;

        if (enemy.cardinalMover != null)
        {
            enemy.cardinalMover.moveSpeed = chargeSpeed;
            enemy.cardinalMover.SetDirection(cardinalDir);
            enemy.cardinalMover.canMove = true;
        }
    }

    private void UpdateCharging()
    {
        // Slammed into a wall — stop, play stun anim, then resume normal logic
        if (IsWallInDirection(chargeDirection))
        {
            if (enemy.cardinalMover != null) enemy.cardinalMover.StopMovement();
            BeginWallStun();
        }
    }

    // ── Wall Stun ───────────────────────────────────────────────────────

    private void BeginWallStun()
    {
        currentState = ChargerState.WallStun;
        phaseTimer = 0f;
        // Direction stays the same so the stun animation still faces the wall.
        SetAnimBools(windup: false, charge: false, stun: true);
        enemy.SuppressIsWalkingAnim = false;
    }

    // Animation event hook — called from the wall-stun clip's last frame.
    public void OnWallStunAnimationEnd()
    {
        if (currentState != ChargerState.WallStun) return;
        SetAnimBools(false, false, false);
        currentState = ChargerState.Scanning;
        
        scanTimer = 0f;
    }

    // ── Approach ────────────────────────────────────────────────────────

    private void BeginApproach(Vector2 cardinalDir)
    {
        currentState = ChargerState.Approaching;
        chargeDirection = cardinalDir;
        redirectTimer = 0f;
        nextRedirectTime = Random.Range(minRedirectInterval, maxRedirectInterval);

        SetAnimBools(false, false, false);

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

        // Player directly ahead — windup + charge
        if (CastRayForPlayer(forward))
        {
            BeginWindup(CardinalToward(playerTransform.position));
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
            // Hit player — windup, then charge in cardinal direction toward them
            BeginWindup(CardinalToward(playerTransform.position));
        }
        else
        {
            // No hit — approach player at normal speed, scan again on arrival
            BeginApproach(CardinalToward(playerTransform.position));
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private void SetAnimBools(bool windup, bool charge, bool stun)
    {
        if (enemy.animator == null) return;
        if (!string.IsNullOrEmpty(windupBoolName)) enemy.animator.SetBool(windupBoolName, windup);
        if (!string.IsNullOrEmpty(chargeBoolName)) enemy.animator.SetBool(chargeBoolName, charge);
        if (!string.IsNullOrEmpty(stunBoolName))   enemy.animator.SetBool(stunBoolName,   stun);
    }

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
        currentState = ChargerState.Windup;
        scanTimer = 0f;
    }
}
