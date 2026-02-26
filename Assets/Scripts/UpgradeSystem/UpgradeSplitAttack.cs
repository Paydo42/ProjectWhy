using UnityEngine;
[CreateAssetMenu(fileName = "UpgradeSplitAttack", menuName = "Upgrades/Split attack Upgrade")]

public class UpgradeSplitAttack : UpgradeSOBase
{
    [Tooltip("Number of additional projectiles to add (e.g., 1 means player shoots 2 total)")]
    [SerializeField] private int additionalProjectiles = 1;
    
    [Tooltip("Spread angle between projectiles in degrees")]
    [SerializeField] private float spreadAngle = 30f;

    public override void ApplyUpgrade(GameObject player)
    {
        PlayerShooting playerShooting = player.GetComponent<PlayerShooting>();
        if (playerShooting != null)
        {
            playerShooting.AddProjectileCount(additionalProjectiles);
            playerShooting.SetSpreadAngle(spreadAngle);
            Debug.Log($"Split Attack upgraded! Now shooting {playerShooting.GetProjectileCount()} projectiles with {spreadAngle}° spread.");
        }
        else
        {
            Debug.LogWarning("PlayerShooting component not found on the player GameObject.");
        }
    }
}
