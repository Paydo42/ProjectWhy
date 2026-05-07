using System;
using UnityEngine;
[CreateAssetMenu(fileName = "UpgradeBouncingProjectile", menuName = "Upgrades/BouncingProjectileUpgrade")]
public class UpgradeBouncingProjectile : UpgradeSOBase
{
    [Tooltip("Number of bounces for the projectile")]
    [SerializeField] private int bounceCount = 3; // Number of bounces for the projectile
            public override int AnimatorTypeId => 9;

    public override void ApplyUpgrade(GameObject player)
    {
        PlayerShooting playerShooting = player.GetComponent<PlayerShooting>();
        if (playerShooting != null)
        {
            playerShooting.EnableBouncingProjectiles(bounceCount);
            Debug.Log("Bouncing projectiles enabled!");
        }
        else
        {
            Debug.LogWarning("PlayerShooting component not found on the player GameObject.");
        }
    }
}
