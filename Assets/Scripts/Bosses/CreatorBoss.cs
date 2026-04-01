using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CreatorBoss : BossBase
{
    [Header("Creator Phase Roots")]
    [SerializeField] private GameObject phase1Root;
    [SerializeField] private GameObject phase2Root;
    [SerializeField] private GameObject phase3Root;

    [Header("Creator Timings")]
    [SerializeField] private float transitionDuration = 1.25f;

    [Header("Creator Events")]
    [SerializeField] private UnityEvent onPhase1Started;
    [SerializeField] private UnityEvent onPhase2Started;
    [SerializeField] private UnityEvent onPhase3Started;
    [SerializeField] private UnityEvent onTransitionStarted;
    [SerializeField] private UnityEvent onCreatorDefeated;

    private Coroutine transitionRoutine;

   
    protected override void Awake()
    {
        base.Awake();
        SetAllPhaseRoots(false);
    }

    protected override void OnEncounterStarted()
    {
        ActivateOnlyRoot(phase1Root);
        onPhase1Started?.Invoke();
        Debug.Log("Creator Boss Encounter Started: Phase 1");
    }

    protected override void OnPhaseChanged(BossPhase newPhase)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(PhaseTransitionRoutine(newPhase));
    }

    protected override void OnBossDeath()
    {
        SetAllPhaseRoots(false);
        onCreatorDefeated?.Invoke();

        // Keep object for death animation/VFX hooks. Disable after effects if desired.
        gameObject.SetActive(false);
    }

    private IEnumerator PhaseTransitionRoutine(BossPhase targetPhase)
    {
        onTransitionStarted?.Invoke();
        SetInvulnerable(true);

        // Brief pause allows animation/camera shakes between phases.
        if (transitionDuration > 0f)
        {
            yield return new WaitForSeconds(transitionDuration);
        }

        switch (targetPhase)
        {
            case BossPhase.Phase1:
                ActivateOnlyRoot(phase1Root);
                onPhase1Started?.Invoke();
                break;

            case BossPhase.Phase2:
                ActivateOnlyRoot(phase2Root);
                onPhase2Started?.Invoke();
                break;

            case BossPhase.Phase3:
                ActivateOnlyRoot(phase3Root);
                onPhase3Started?.Invoke();
                break;
        }

        SetInvulnerable(false);
        transitionRoutine = null;
    }

    private void ActivateOnlyRoot(GameObject activeRoot)
    {
        if (phase1Root != null) phase1Root.SetActive(phase1Root == activeRoot);
        if (phase2Root != null) phase2Root.SetActive(phase2Root == activeRoot);
        if (phase3Root != null) phase3Root.SetActive(phase3Root == activeRoot);
    }

    private void SetAllPhaseRoots(bool isActive)
    {
        if (phase1Root != null) phase1Root.SetActive(isActive);
        if (phase2Root != null) phase2Root.SetActive(isActive);
        if (phase3Root != null) phase3Root.SetActive(isActive);
    }
}
