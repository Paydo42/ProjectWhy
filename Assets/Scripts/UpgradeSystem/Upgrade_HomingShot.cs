using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade_HomingShot", menuName = "Upgrades/Homing Shot Upgrade")]
public class Upgrade_HomingShot : UpgradeSOBase
{
    [Tooltip("How fast the projectile steers toward the target (higher = tighter turns)")]
    [SerializeField] private float homingStrength = 5f;
    
    [Tooltip("Detection radius to find the nearest enemy")]
    [SerializeField] private float homingRange = 8f;
    
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
    }
    
}
