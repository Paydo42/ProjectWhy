using UnityEngine;
[CreateAssetMenu(fileName = "UpgradePiercingProjectiles", menuName = "Upgrades/Piercing Projectiles Upgrade")]
public class UpgradePiercingProjectiles : UpgradeSOBase
{
    [Tooltip("Number of additional enemies the projectile can pierce through (1 = hits 2 enemies total)")]
    [SerializeField] private int pierceCount = 1;
    
    public override void ApplyUpgrade(GameObject player)
    {
        PlayerShooting playerShooting = player.GetComponent<PlayerShooting>();
        if (playerShooting != null)
        {
            playerShooting.AddPierceCount(pierceCount);
            Debug.Log($"Piercing upgraded! Projectiles now pierce through {playerShooting.GetPierceCount()} enemies.");
        }
        else
        {
            Debug.LogWarning("PlayerShooting component not found on the player GameObject.");
        }
    }
}
