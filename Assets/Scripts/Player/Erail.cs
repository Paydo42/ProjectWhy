using UnityEngine;

public class Erail : MonoBehaviour
{
    [Header("references")]
    [SerializeField] private Transform arrowSpawnPoint;

    [Header("visual settings")]
    [SerializeField] private float rotationOffset = 0f;
    [Header("Audio ")]
    [SerializeField] private AudioClip shootClip;
    private AudioSource audioSource;

    private GameObject projectilePrefab;
    private Vector2 shootDirection;
    private float projectileSpeed;
    private GameObject originalPrefab;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    public void Initialize(GameObject bowPrefabRef,GameObject projectilePrefab, Vector2 shootDirection, float projectileSpeed)
    {
        this.originalPrefab = bowPrefabRef;
        this.projectilePrefab = projectilePrefab;
        this.shootDirection = shootDirection;
        this.projectileSpeed = projectileSpeed;

      // GetComponent <Animator>().Play("Shoot", -1, 0f);
    }
    public void TriggerShootEvent()
    {
        if (projectilePrefab == null || arrowSpawnPoint == null) return;

        Quaternion arrowRotation = transform.rotation *  Quaternion.Euler(0, 0, rotationOffset);
        GameObject arrowObj = PoolManager.Instance.Spawn(projectilePrefab, arrowSpawnPoint.position, arrowRotation);
      
        if (audioSource != null && shootClip != null)
        {
            audioSource.PlayOneShot(shootClip);
        }


        Projectile projectile = arrowObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(projectilePrefab);
            projectile.SetDirection(shootDirection, projectileSpeed);
        }
    }
    public void DisableErail()
    {
       if (PoolManager.Instance != null && originalPrefab != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject, originalPrefab);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

}
