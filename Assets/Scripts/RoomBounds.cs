using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomBounds : MonoBehaviour
{
    [Header("Enemy Spawning")]
    public RoomLayout roomLayout; // Assign your layout file here in the Inspector!
    private List<Enemy> activeEnemies = new List<Enemy>(); // List to track spawned enemies

    [Header("Camera Settings")]
    private BoxCollider2D BoxCollider; // Renamed from 'box'

    [Header("State Tracking")]
    private bool rewardSpawned = false;
    private bool roomActive = false;
    private bool playerIsInRoom = false;

    [Header("Door Settings")]
    public List<Door> roomDoors = new List<Door>();

    [Header("Reward Settings")]
    public int minRewards = 2;
    public int maxRewards = 3;
    public List<GameObject> spawnablePrefabs = new List<GameObject>(); // For rewards, not enemies
    public Transform[] spawnPoints;
    [Range(0f, 1f)] public float spawnChance = 1f;

    private List<Upgrade> currentUpgrades = new List<Upgrade>();
    private BoiCameraController cameraController;

    void Awake()
    {
        BoxCollider = GetComponent<BoxCollider2D>();
        BoxCollider.isTrigger = true;
        cameraController = Camera.main.GetComponent<BoiCameraController>();

        // We no longer find pre-placed enemies here
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerIsInRoom = true;

        Rect bounds = new Rect(
            BoxCollider.bounds.min.x,
            BoxCollider.bounds.min.y,
            BoxCollider.bounds.size.x,
            BoxCollider.bounds.size.y
        );

        if (cameraController != null)
            cameraController.SetActiveBounds(bounds);
    }

    void Update()
    {
        // Check if the player is in the room, the room isn't active yet,
        // and there's a layout with enemies defined.
        if (playerIsInRoom && !roomActive && roomLayout != null && roomLayout.enemiesToSpawn.Count > 0)
        {
            // Check if the player's CENTER is inside the bounds
            if (BoxCollider.bounds.Contains(Player.Instance.transform.position))
            {
                ActivateRoom(); // Activate and spawn enemies
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerIsInRoom = false;

        // Optional: If you want enemies to despawn if the player leaves mid-fight
        // ResetRoom();
    }

    private void ActivateRoom()
    {
        if (roomActive) return; // Prevent double activation

        roomActive = true;
        LockDoors();
        SpawnEnemies(); // Spawn enemies using the pool
        Debug.Log($"Room activated: {gameObject.name}");
    }

    // --- NEW METHOD: Spawns enemies using the PoolManager ---
    private void SpawnEnemies()
    {
        activeEnemies.Clear(); // Clear list from previous activations if any

        if (roomLayout == null || PoolManager.Instance == null)
        {
            Debug.LogWarning($"Room {gameObject.name} has no RoomLayout assigned or PoolManager is missing.");
            // If there are no enemies, the room should just be clear
            if (roomDoors.Count > 0) UnlockDoors(); // Unlock doors immediately
            return;
        }

        foreach (var placement in roomLayout.enemiesToSpawn)
        {
            if (placement.enemyPrefab == null) continue;

            // Calculate world position based on room position + layout offset
            Vector3 spawnPos = transform.position + (Vector3)placement.position;

            // Spawn from pool
            GameObject enemyObj = PoolManager.Instance.Spawn(placement.enemyPrefab, spawnPos, Quaternion.identity);
            Enemy enemy = enemyObj.GetComponent<Enemy>();

            if (enemy != null)
            {
                activeEnemies.Add(enemy);
                enemy.Activate(this, placement.enemyPrefab); // Activate the enemy's AI
                enemy.OnEnemyDeath += HandleEnemyDeath; // Subscribe to its death event
            }
            else
            {
                Debug.LogError($"Prefab {placement.enemyPrefab.name} does not have an Enemy component!");
                // Return the object to pool if it's not a valid enemy
                 PoolManager.Instance.ReturnToPool(enemyObj, placement.enemyPrefab);
            }
        }

        Debug.Log($"Spawned {activeEnemies.Count} enemies in room {gameObject.name}.");

        // If after trying to spawn, there are actually no enemies, unlock doors.
         if (activeEnemies.Count == 0 && !rewardSpawned)
         {
             Debug.Log($"Room {gameObject.name} activated but had no valid enemies to spawn. Unlocking doors.");
             UnlockDoors();
             // Optionally spawn reward immediately if layout was empty
             // RoomCleared();
         }
    }

    private void HandleEnemyDeath(Enemy deadEnemy)
    {
         if (deadEnemy != null) // Check if the enemy reference is valid
         {
             // Unsubscribe FIRST to prevent issues if HandleEnemyDeath is called multiple times
             deadEnemy.OnEnemyDeath -= HandleEnemyDeath;

             if (activeEnemies.Contains(deadEnemy))
             {
                 activeEnemies.Remove(deadEnemy);
                 Debug.Log($"Enemy defeated: {deadEnemy.name}, Remaining enemies: {activeEnemies.Count}");
             }
         }


        // Check if the room is now clear
        if (activeEnemies.Count == 0 && !rewardSpawned)
        {
            RoomCleared();
            Debug.Log("All enemies defeated, spawning reward.");
        }
    }

    private void RoomCleared()
    {
        Debug.Log("Room cleared! Unlocking doors and spawning reward.");
        UnlockDoors();
        SpawnRandomReward();
        rewardSpawned = true;
        // Keep roomActive = true so it doesn't re-trigger spawning
    }

    // Optional: Method to despawn enemies and reset the room
    public void ResetRoom()
    {
        Debug.Log($"Resetting room {gameObject.name}");
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.OnEnemyDeath -= HandleEnemyDeath; // Unsubscribe
                 if (PoolManager.Instance != null && enemy.OriginalPrefab != null)
                 {
                     PoolManager.Instance.ReturnToPool(enemy.gameObject, enemy.OriginalPrefab);
                 }
                 else
                 {
                     enemy.gameObject.SetActive(false); // Fallback
                 }
            }
        }
        activeEnemies.Clear();
        ClearExistingRewards();
        UnlockDoors(); // Or keep them locked depending on desired behavior
        roomActive = false;
        rewardSpawned = false;
    }


    private void LockDoors()
    {
        foreach (Door door in roomDoors)
        {
            if (door != null) door.CloseAndLock();
        }
    }

    private void UnlockDoors()
    {
        foreach (Door door in roomDoors)
        {
            if (door != null) door.Open();
        }
    }

    // --- Reward Spawning Logic (Remains the same) ---
    private void SpawnRandomReward()
    {
        if (Random.value > spawnChance) return;
        if (spawnablePrefabs.Count == 0) return;

        ClearExistingRewards();
        int rewardCount = Random.Range(minRewards, maxRewards + 1);
        List<Transform> availableSpawnPoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < rewardCount; i++)
        {
            if (availableSpawnPoints.Count == 0 || spawnablePrefabs.Count == 0) break;

            int prefabIndex = Random.Range(0, spawnablePrefabs.Count);
            GameObject prefabToSpawn = spawnablePrefabs[prefabIndex];

            int spawnIndex = Random.Range(0, availableSpawnPoints.Count);
            Transform spawnPoint = availableSpawnPoints[spawnIndex];
            availableSpawnPoints.RemoveAt(spawnIndex);

            GameObject reward = Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity, transform);

            Vector3 fixedPos = reward.transform.position;
            fixedPos.z = 0f;
            reward.transform.position = fixedPos;

            Upgrade upgrade = reward.GetComponent<Upgrade>();
            if (upgrade != null)
            {
                currentUpgrades.Add(upgrade);
            }
        }
    }

    public void ClearExistingRewards()
    {
        foreach (Upgrade upgrade in currentUpgrades)
        {
            if (upgrade != null && upgrade.gameObject != null)
            {
                Destroy(upgrade.gameObject);
            }
        }
        currentUpgrades.Clear();
    }

    public void PlayerSelectedUpgrade(Upgrade selectedUpgrade)
    {
         foreach (Upgrade upgrade in currentUpgrades)
         {
             if (upgrade != selectedUpgrade && upgrade != null && upgrade.gameObject != null)
             {
                 Animator anim = upgrade.GetComponent<Animator>();
                 if (anim != null)
                 {
                     anim.SetTrigger("Disappear");
                     // Optionally destroy after animation: Destroy(upgrade.gameObject, anim.GetCurrentAnimatorStateInfo(0).length);
                 }
                 else
                 {
                     Destroy(upgrade.gameObject);
                 }
             }
         }
         // Clear the list *after* iterating and potentially destroying
         currentUpgrades.RemoveAll(u => u == null || u.gameObject == null || u == selectedUpgrade);

         // Destroy the selected upgrade *after* clearing others if it shouldn't persist
         if (selectedUpgrade != null && selectedUpgrade.gameObject != null)
         {
              // Decide if the selected upgrade should also disappear or be destroyed
              // Destroy(selectedUpgrade.gameObject);
         }
    }

    private void OnDestroy()
    {
        // Ensure we unsubscribe if the room is destroyed mid-fight
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.OnEnemyDeath -= HandleEnemyDeath;
            }
        }
    }
}