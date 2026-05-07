using UnityEngine;

// Forwards animation events fired by the laser visual's Animator to the
// LaserBeam component, which usually sits on a parent GameObject.
//
// Place this component on the SAME GameObject as the Animator that plays
// the laser clip. The clip's animation events should call EnableHitbox /
// DisableHitbox / Despawn — they will be routed to the LaserBeam.
public class LaserBeamAnimationEvents : MonoBehaviour
{
    [Tooltip("Auto-found via GetComponentInParent if left null.")]
    [SerializeField] private LaserBeam target;

    private void Awake()
    {
        if (target == null) target = GetComponentInParent<LaserBeam>(true);
    }

    public void EnableHitbox()
    {
        if (target != null) target.EnableHitbox();
    }

    public void DisableHitbox()
    {
        if (target != null) target.DisableHitbox();
    }

    public void Despawn()
    {
        if (target != null) target.Despawn();
    }
}
