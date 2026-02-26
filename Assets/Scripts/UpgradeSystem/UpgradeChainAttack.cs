using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeChainAttack", menuName = "Upgrades/Chain Attack Upgrade")]
public class UpgradeChainAttack : UpgradeSOBase
{
    [Tooltip("How many enemies the projectile chains to")]
    [SerializeField] private int chainCount = 2;
    
    [Tooltip("Range to search for next enemy to chain to")]
    [SerializeField] private float chainRange = 5f;
    
    [Tooltip("Damage multiplier for each chain (0.8 = 80% damage per chain)")]
    [SerializeField] private float chainDamageMultiplier = 0.8f;
    
    public override void ApplyUpgrade(GameObject player)
    {
        PlayerShooting playerShooting = player.GetComponent<PlayerShooting>();
        if (playerShooting != null)
        {
            playerShooting.EnableChainAttack(chainCount, chainRange, chainDamageMultiplier);
            Debug.Log($"Chain Attack enabled! Chains to {chainCount} enemies within {chainRange} range.");
        }
        else
        {
            Debug.LogWarning("PlayerShooting component not found on the player GameObject.");
        }
    }
}
