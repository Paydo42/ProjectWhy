using UnityEngine;

public abstract class UpgradeSOBase : ScriptableObject
{
    public abstract void ApplyUpgrade(GameObject player);

    // Animator "UpgradeType" float used by the pillar/pickup visuals.
    // Override in each concrete upgrade to assign its own ID.
    public virtual int AnimatorTypeId => 0;
}