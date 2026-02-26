using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeTripleAttack", menuName = "Upgrades/Triple attack Upgrade")]
public class UpgradeTripleAttack : UpgradeSOBase
{
    [SerializeField] private int additionalProjectiles = 2;
    
    [Tooltip("Spread angle between projectiles in degrees")]
    [SerializeField] private float spreadAngle = 15f;
    public override void ApplyUpgrade(GameObject player)
    {
        PlayerShooting playerShooting = player.GetComponent<PlayerShooting>();
        if (playerShooting != null)
        {
            playerShooting.AddProjectileCount(additionalProjectiles); // Add 2 additional projectiles for a total of 3
            playerShooting.SetSpreadAngle(spreadAngle); // Set a default spread angle for triple shot
            Debug.Log($"Triple Attack upgraded! Now shooting {playerShooting.GetProjectileCount()} projectiles with 30° spread.");
        }
        else
        {
            Debug.LogWarning("PlayerShooting component not found on the player GameObject.");
        }
    }
}