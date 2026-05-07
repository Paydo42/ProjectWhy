using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade_HomingShot", menuName = "Upgrades/Homing Shot Upgrade")]
public class Upgrade_HomingShot : UpgradeSOBase
{
    [Tooltip("How fast the projectile steers toward the target (higher = tighter turns)")]
    [SerializeField] private float homingStrength = 5f;
    public override int AnimatorTypeId => 8;

    [Tooltip("Detection radius to find the nearest enemy")]
    [SerializeField] private float homingRange = 8f;

    [Header("Follower Visual")]
    [Tooltip("Prefab spawned as a child of the player so it follows them around. Leave null for no visual.")]
    [SerializeField] private GameObject followerPrefab;
    [Tooltip("Local position offset from the player.")]
    [SerializeField] private Vector3 followerLocalOffset = Vector3.zero;

    public override void ApplyUpgrade(GameObject player)
    {
        PlayerShooting playerShooting = player.GetComponent<PlayerShooting>();
        if (playerShooting != null)
        {
            playerShooting.EnableHoming(homingStrength, homingRange);
            Debug.Log($"Homing Shot enabled! Strength: {homingStrength}, Range: {homingRange}");
        }
        else
        {
            Debug.LogWarning("PlayerShooting component not found on the player GameObject.");
        }

        if (followerPrefab != null)
        {
            GameObject follower = Instantiate(followerPrefab, player.transform);
            follower.transform.localPosition = followerLocalOffset;

            // Drive the existing Upgrade animator's blend tree so the follower
            // shows the homing-shot icon instead of whatever frame 0 happens to be.
            if (follower.TryGetComponent(out Animator followerAnimator))
                followerAnimator.SetFloat("UpgradeType", AnimatorTypeId);
        }
    }
}
