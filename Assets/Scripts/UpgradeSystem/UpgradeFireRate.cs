using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeFireRate", menuName = "Upgrades/Fire Rate Upgrade")]
public class UpgradeFireRate : UpgradeSOBase
{
    [Tooltip("Amount to increase fire rate by (e.g., 0.2 means 20% faster)")]
    [SerializeField] private float fireRateMultiplier = 0.2f;

    public override void ApplyUpgrade(GameObject player)
    {
        PlayerShooting playerShooting = player.GetComponent<PlayerShooting>();
        if (playerShooting != null)
        {
            playerShooting.IncreaseFireRate(fireRateMultiplier);
            Debug.Log($"Fire Rate upgraded! New fire rate multiplier: {fireRateMultiplier}");
        }
        else
        {
            Debug.LogWarning("PlayerShooting component not found on the player GameObject.");
        }
    }
}
