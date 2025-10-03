using UnityEngine;
[CreateAssetMenu(fileName = "UpgradeSpeed", menuName = "Upgrades/Speed Upgrade")]
public class UpgradeSpeed : UpgradeSOBase
{
    [SerializeField] private float speedIncrease;

    public override void ApplyUpgrade(GameObject player)
    {
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.moveSpeed += speedIncrease;
            Debug.Log($"Speed increased by {speedIncrease}. New speed: {playerMovement.moveSpeed}");
        }
        else
        {
            Debug.LogWarning("PlayerMovement component not found on the player GameObject.");
        }
    }
}

