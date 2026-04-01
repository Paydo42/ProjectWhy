using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(Collider2D))]
public class BossDoorEncounterTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Door bossDoor;
    [SerializeField, Tooltip("Scene boss reference. If missing, an instance can be spawned from Boss Prefab.")]
    private BossBase boss;
    [SerializeField, Tooltip("Optional prefab used when no scene boss is assigned.")]
    private BossBase bossPrefab;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private PlayableDirector cutsceneDirector;

    [Header("Encounter Doors")]
    [SerializeField] private bool lockBossDoor = true;
    [SerializeField] private List<Door> doorsToLockDuringEncounter = new List<Door>();
    [SerializeField] private bool waitForPlayerToExitTriggerBeforeLock = true;
    [SerializeField] private float maxWaitForTriggerExit = 2f;
    [SerializeField] private float lockDelayAfterEncounterStart = 0.5f;

    [Header("Cutscene")]
    [SerializeField] private bool requireInteractToStart = false;
    [SerializeField] private bool disablePlayerControlDuringCutscene = true;
    [SerializeField] private float fallbackCutsceneDuration = 2f;

    [Header("Boss Spawn")]
    [SerializeField] private bool activateBossGameObjectOnStart = true;
    [SerializeField] private bool spawnBossFromPrefabIfMissing = true;
    [SerializeField] private bool hideSpawnedBossUntilEncounterStart = true;
    [SerializeField] private float cutsceneFailSafeStartDelay = 0.25f;

    private static BossDoorEncounterTrigger activeDoorInRange;

    private bool playerInRange;
    private bool encounterStarted;
    private bool bossEncounterStarted;
    private PlayerMovement cachedPlayerMovement;
    private PlayerShooting cachedPlayerShooting;
    private BossBase runtimeSpawnedBoss;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        EnsureBossInstance(false);

        if (boss != null)
        {
            boss.OnBossDefeated += HandleBossDefeated;
        }
    }

    private void OnDestroy()
    {
        if (boss != null)
        {
            boss.OnBossDefeated -= HandleBossDefeated;
        }

        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped -= OnCutsceneStopped;
        }

        if (runtimeSpawnedBoss != null)
        {
            runtimeSpawnedBoss.OnBossDefeated -= HandleBossDefeated;
        }

        if (activeDoorInRange == this)
        {
            activeDoorInRange = null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[BossTrigger] Something entered: {other.name} tag:{other.tag}");
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = true;
        activeDoorInRange = this;

        if (cachedPlayerMovement == null)
        {
            cachedPlayerMovement = other.GetComponent<PlayerMovement>();
        }

        if (cachedPlayerShooting == null)
        {
            cachedPlayerShooting = other.GetComponent<PlayerShooting>();
        }

        // Default flow: no interaction required, entering trigger starts boss sequence.
        Debug.Log($"[BossTrigger] requireInteractToStart:{requireInteractToStart} encounterStarted:{encounterStarted}");
        if (!requireInteractToStart)
        {
            TryStartEncounterFromDoor();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = false;
        if (activeDoorInRange == this)
        {
            activeDoorInRange = null;
        }
    }

    public static bool TryUseCurrentDoor()
    {
        if (activeDoorInRange == null)
        {
            return false;
        }

        return activeDoorInRange.TryStartEncounterFromDoor();
    }

    private bool TryStartEncounterFromDoor()
    {
        Debug.Log($"[BossTrigger] TryStart — playerInRange:{playerInRange} encounterStarted:{encounterStarted} boss:{boss}");
        if (!playerInRange || encounterStarted)
        {
            return false;
        }

        if (!EnsureBossInstance(true))
        {
            Debug.LogError($"{name}: No boss available. Assign a scene boss or Boss Prefab.", this);
            return false;
        }

        encounterStarted = true;
        StartCoroutine(CutsceneAndEncounterRoutine());
        return true;
    }

    private IEnumerator CutsceneAndEncounterRoutine()
    {
        if (disablePlayerControlDuringCutscene)
        {
            SetPlayerControlEnabled(false);
        }

        Debug.Log($"[BossTrigger] Routine — cutsceneDirector:{cutsceneDirector} fallbackDuration:{fallbackCutsceneDuration}");
        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped += OnCutsceneStopped;
            cutsceneDirector.Play();
            // Fail-safe in case timeline never fires the stopped callback.
            if (cutsceneFailSafeStartDelay >= 0f)
            {
                StartCoroutine(CutsceneFailSafeRoutine());
            }
            yield break;
        }

        if (fallbackCutsceneDuration > 0f)
        {
            yield return new WaitForSeconds(fallbackCutsceneDuration);
        }

        Debug.Log("[BossTrigger] Calling StartBossEncounter");
        StartBossEncounter();
    }

    private void OnCutsceneStopped(PlayableDirector director)
    {
        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped -= OnCutsceneStopped;
        }

        StartBossEncounter();
    }

    private void StartBossEncounter()
    {
        if (bossEncounterStarted)
        {
            return;
        }

        bossEncounterStarted = true;

        if (!EnsureBossInstance(true))
        {
            Debug.LogError($"{name}: Cannot start encounter because boss is missing.", this);
            if (disablePlayerControlDuringCutscene)
            {
                SetPlayerControlEnabled(true);
            }
            return;
        }

        bool bossActive = boss != null && boss.gameObject.activeSelf;
        Debug.Log($"[BossTrigger] boss:{boss} activeSelf:{bossActive} activateBossOnStart:{activateBossGameObjectOnStart}");
        if (activateBossGameObjectOnStart && boss != null && !boss.gameObject.activeSelf)
        {
            boss.gameObject.SetActive(true);
        }

        Debug.Log($"[BossTrigger] Calling boss.StartEncounter on {(boss != null ? boss.name : "NULL")}");
        boss.StartEncounter();

        if (disablePlayerControlDuringCutscene)
        {
            SetPlayerControlEnabled(true);
        }

        StartCoroutine(LockDoorsAfterStartRoutine());
    }

    private IEnumerator CutsceneFailSafeRoutine()
    {
        yield return null;

        if (cutsceneDirector == null)
        {
            yield break;
        }

        while (cutsceneDirector.state == PlayState.Playing)
        {
            yield return null;
        }

        if (bossEncounterStarted)
        {
            yield break;
        }

        if (cutsceneFailSafeStartDelay > 0f)
        {
            yield return new WaitForSeconds(cutsceneFailSafeStartDelay);
        }

        StartBossEncounter();
    }

    private IEnumerator LockDoorsAfterStartRoutine()
    {
        if (waitForPlayerToExitTriggerBeforeLock)
        {
            float timer = 0f;
            while (playerInRange && timer < maxWaitForTriggerExit)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        if (lockDelayAfterEncounterStart > 0f)
        {
            yield return new WaitForSeconds(lockDelayAfterEncounterStart);
        }

        LockEncounterDoors();
    }

    private void HandleBossDefeated(BossBase _) 
    {
        UnlockEncounterDoors();
    }

    private void LockEncounterDoors()
    {
        if (lockBossDoor && bossDoor != null)
        {
            bossDoor.CloseAndLock();
        }

        foreach (Door door in doorsToLockDuringEncounter)
        {
            if (door != null)
            {
                door.CloseAndLock();
            }
        }
    }

    private void UnlockEncounterDoors()
    {
        if (bossDoor != null)
        {
            bossDoor.Open();
        }

        foreach (Door door in doorsToLockDuringEncounter)
        {
            if (door != null)
            {
                door.Open();
            }
        }
    }

    private void SetPlayerControlEnabled(bool isEnabled)
    {
        if (cachedPlayerMovement != null)
        {
            cachedPlayerMovement.enabled = isEnabled;
        }

        if (cachedPlayerShooting != null)
        {
            cachedPlayerShooting.enabled = isEnabled;
        }
    }

    private bool EnsureBossInstance(bool allowSpawn)
    {
        if (boss != null)
        {
            return true;
        }

        if (!allowSpawn || !spawnBossFromPrefabIfMissing || bossPrefab == null)
        {
            return false;
        }

        Vector3 spawnPosition = bossSpawnPoint != null ? bossSpawnPoint.position : transform.position;
        Quaternion spawnRotation = bossSpawnPoint != null ? bossSpawnPoint.rotation : Quaternion.identity;

        runtimeSpawnedBoss = Instantiate(bossPrefab, spawnPosition, spawnRotation);
        boss = runtimeSpawnedBoss;
        boss.OnBossDefeated += HandleBossDefeated;

        if (hideSpawnedBossUntilEncounterStart)
        {
            boss.gameObject.SetActive(false);
        }

        Debug.Log($"{name}: Spawned runtime boss instance '{boss.name}'.", this);
        return true;
    }
}
