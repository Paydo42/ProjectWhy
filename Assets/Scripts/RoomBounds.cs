using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomBounds : MonoBehaviour
{
    [Header("Camera Settings")]
    private BoxCollider2D BoxCollider;

    [Header("Enemy Tracking")]
    private List<Enemy> enemiesInRoom = new List<Enemy>();
    private bool rewardSpawned = false;
    private bool roomActive = false;
    private bool playerIsInRoom = false; // --- We will use this to run a check

    [Header("Door Settings")]
    public List<Door> roomDoors = new List<Door>();
    
    [Header("Reward Settings")]
    public int minRewards = 2;
    public int maxRewards = 3;
    public List<GameObject> spawnablePrefabs = new List<GameObject>();
    public Transform[] spawnPoints;
    [Range(0f, 1f)] public float spawnChance = 1f;

    private List<Upgrade> currentUpgrades = new List<Upgrade>();
    private BoiCameraController cameraController;

    void Awake()
    {
        BoxCollider = GetComponent<BoxCollider2D>();
        BoxCollider.isTrigger = true; 
        FindAllEnemiesInRoom();
        cameraController = Camera.main.GetComponent<BoiCameraController>();
    }

    private void FindAllEnemiesInRoom()
    {
        Enemy[] foundEnemies = GetComponentsInChildren<Enemy>(true);
        enemiesInRoom.AddRange(foundEnemies);
        Debug.Log($"Found {enemiesInRoom.Count} enemies in room: {gameObject.name}");
        foreach (Enemy enemy in enemiesInRoom)
        {
            enemy.OnEnemyDeath += HandleEnemyDeath;
        }
        if (enemiesInRoom.Count == 0)
        {
            UnlockDoors();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerIsInRoom = true; // --- We just set this to TRUE
        
        // take bounds from the BoxCollider collider
        Rect bounds = new Rect(
            BoxCollider.bounds.min.x,
            BoxCollider.bounds.min.y,
            BoxCollider.bounds.size.x,
            BoxCollider.bounds.size.y
        );

        // Set camera bounds
        if (cameraController != null)
            cameraController.SetActiveBounds(bounds);
        
        // --- We NO LONGER activate the room here.
    }

    // --- NEW UPDATE FUNCTION ---
    // We will check every frame if the player is in the zone and if we should activate
    void Update()
    {
        // Do nothing if player isn't in the trigger, or room is already active, or no enemies
        if (!playerIsInRoom || roomActive || enemiesInRoom.Count == 0)
        {
            return;
        }

        // --- THE FIX ---
        // Check if the player's CENTER is inside the BoxCollider bounds
        if (BoxCollider.bounds.Contains(Player.Instance.transform.position))
        {
            ActivateRoom();
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerIsInRoom = false; // --- We just set this to FALSE
    }

    // --- This is now a PRIVATE function again
    private void ActivateRoom()
    {
        roomActive = true;
        LockDoors();
        Debug.Log($"Room activated: {gameObject.name}, Enemies: {enemiesInRoom.Count}");
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
    
    private void HandleEnemyDeath(Enemy deadEnemy)
    {
        enemiesInRoom.Remove(deadEnemy);
        Debug.Log($"Enemy defeated: {deadEnemy.name}, Remaining enemies: {enemiesInRoom.Count}");

        if (enemiesInRoom.Count == 0 && !rewardSpawned)
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
        roomActive = false;
    }
    
    public bool ContainsPlayer()
    {
        if (!playerIsInRoom) return false;
        Vector2 playerPosition = new Vector2(Player.Instance.transform.position.x, Player.Instance.transform.position.y);
        return BoxCollider.bounds.Contains(playerPosition);
    }
    
    
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
            if (upgrade != selectedUpgrade && upgrade != null)
            {
                Animator anim = upgrade.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetTrigger("Disappear");
                }
                else
                {
                    Destroy(upgrade.gameObject);
                }
            }
        }
        currentUpgrades.Clear();
    }
    
    private void OnDestroy()
    {
        foreach (Enemy enemy in enemiesInRoom)
        {
            if (enemy != null)
            {
                enemy.OnEnemyDeath -= HandleEnemyDeath;
                Debug.Log($"Unsubscribed from enemy death event for: {enemy.name}");
            }
        }
    }
    
    public void DebugRoomInfo()
    {
        Debug.Log($"Room: {gameObject.name}, Active: {roomActive}, Enemies: {enemiesInRoom.Count}, RewardSpawned: {rewardSpawned}");
    }
}