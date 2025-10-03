using UnityEngine;
[CreateAssetMenu(fileName = "UpgradeHealth", menuName = "Upgrades/Health Upgrade")]
public class UpgradeHealth : UpgradeSOBase
{

    [SerializeField] private int healthIncrease;
    public override void ApplyUpgrade(GameObject player)
    {
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();


        if (playerHealth != null)
        {
            playerHealth.currentHealth += healthIncrease;
            playerHealth.maxHealth += healthIncrease; // Ensure max health is also increased
            playerHealth.healthUiManager.DrawHearts(); // Call a method to update the health UI if necessary
            Debug.Log($"Health increased by {healthIncrease}. New health: {playerHealth.currentHealth}");
        }
        else
        {
            Debug.LogWarning("PlayerHealth component not found on the player GameObject.");
        }
    }

}
