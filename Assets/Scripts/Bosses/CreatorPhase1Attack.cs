using System.Collections;
using UnityEngine;

public class CreatorPhase1Attack : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private int projectileDamage = 1;

    [Header("Spiral Attack")]
    [SerializeField] private float spiralFireRate = 0.08f;
    [SerializeField] private int spiralBulletsPerRotation = 36;
    [SerializeField] private int spiralArms = 3;
    [SerializeField] private float spiralDuration = 4f;

    [Header("Burst Attack")]
    [SerializeField] private int burstBulletCount = 16;
    [SerializeField] private int burstWaves = 3;
    [SerializeField] private float burstWaveDelay = 0.6f;

    [Header("Aimed Attack")]
    [SerializeField] private int aimedShotCount = 5;
    [SerializeField] private float aimedShotDelay = 0.25f;
    [SerializeField] private float aimedSpreadAngle = 15f;

    [Header("Pattern Timing")]
    [SerializeField] private float delayBetweenPatterns = 1.5f;

    private Transform player;
    private Coroutine attackRoutine;

    private void OnEnable()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        attackRoutine = StartCoroutine(AttackLoop());
    }

    private void OnDisable()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
    }

    private IEnumerator AttackLoop()
    {
        // Small delay before first attack
        yield return new WaitForSeconds(1f);

        while (true)
        {
            // Spiral attack
            yield return StartCoroutine(SpiralAttack());
            yield return new WaitForSeconds(delayBetweenPatterns);

            // Circle burst attack
            yield return StartCoroutine(BurstAttack());
            yield return new WaitForSeconds(delayBetweenPatterns);

            // Aimed shots at player
            yield return StartCoroutine(AimedAttack());
            yield return new WaitForSeconds(delayBetweenPatterns);
        }
    }

    /// <summary>
    /// Fires rotating spiral arms of bullets.
    /// </summary>
    private IEnumerator SpiralAttack()
    {
        float angleStep = 360f / spiralBulletsPerRotation;
        float currentAngle = 0f;
        int totalShots = Mathf.RoundToInt(spiralDuration / spiralFireRate);

        for (int i = 0; i < totalShots; i++)
        {
            for (int arm = 0; arm < spiralArms; arm++)
            {
                float angle = currentAngle + (360f / spiralArms) * arm;
                Vector2 dir = AngleToDirection(angle);
                SpawnProjectile(dir);
            }

            currentAngle += angleStep;
            yield return new WaitForSeconds(spiralFireRate);
        }
    }

    /// <summary>
    /// Fires expanding rings of bullets.
    /// </summary>
    private IEnumerator BurstAttack()
    {
        float angleOffset = 0f;

        for (int wave = 0; wave < burstWaves; wave++)
        {
            for (int i = 0; i < burstBulletCount; i++)
            {
                float angle = angleOffset + (360f / burstBulletCount) * i;
                Vector2 dir = AngleToDirection(angle);
                SpawnProjectile(dir);
            }

            // Offset each wave so bullets fill gaps
            angleOffset += (360f / burstBulletCount) * 0.5f;
            yield return new WaitForSeconds(burstWaveDelay);
        }
    }

    /// <summary>
    /// Fires rapid aimed shots toward the player with slight spread.
    /// </summary>
    private IEnumerator AimedAttack()
    {
        for (int i = 0; i < aimedShotCount; i++)
        {
            if (player != null)
            {
                Vector2 dirToPlayer = ((Vector2)(player.position - transform.position)).normalized;
                float baseAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
                float spread = Random.Range(-aimedSpreadAngle, aimedSpreadAngle);
                Vector2 dir = AngleToDirection(baseAngle + spread);
                SpawnProjectile(dir);
            }

            yield return new WaitForSeconds(aimedShotDelay);
        }
    }

    private void SpawnProjectile(Vector2 direction)
    {
        if (projectilePrefab == null || PoolManager.Instance == null) return;

        GameObject bullet = PoolManager.Instance.Spawn(projectilePrefab, transform.position, Quaternion.identity);
        EnemyProjectile ep = bullet.GetComponent<EnemyProjectile>();

        if (ep != null)
        {
            ep.damage = projectileDamage;
            ep.Initialize(projectilePrefab);
            ep.SetDirection(direction, projectileSpeed);
        }
    }

    private Vector2 AngleToDirection(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}
