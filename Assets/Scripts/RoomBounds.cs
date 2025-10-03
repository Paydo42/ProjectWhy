using UnityEngine;
using System.Collections.Generic;
using System.Numerics;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomBounds : MonoBehaviour
{
    [Header("Camera Settings")]
    private BoxCollider2D box;

    [Header("Enemy Tracking")]
    private List<Enemy> enemiesInRoom = new List<Enemy>();
    private bool rewardSpawned = false;

    [Header("Reward Settings")]
    public int minRewards = 2;
    public int maxRewards = 3;
    public List<GameObject> spawnablePrefabs = new List<GameObject>();
    public Transform[] spawnPoints;
    [Range(0f, 1f)] public float spawnChance = 1f;

    private List<Upgrade> currentUpgrades = new List<Upgrade>();
    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;  // make sure the collider is a trigger
        FindAllEnemiesInRoom();
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

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // take bounds from the box collider
        Rect bounds = new Rect(
            box.bounds.min.x,
            box.bounds.min.y,
            box.bounds.size.x,
            box.bounds.size.y
        );


        var camCtrl = Camera.main.GetComponent<BoiCameraController>();
        if (camCtrl != null)
            camCtrl.SetActiveBounds(bounds);
    }
    private void HandleEnemyDeath(Enemy deadEnemy)
    {
        enemiesInRoom.Remove(deadEnemy);


        if (enemiesInRoom.Count == 0 && !rewardSpawned)
        {
            SpawnRandomReward();
            rewardSpawned = true;
            Debug.Log("All enemies defeated, spawning reward.");
            
        }
    }
    private void SpawnRandomReward()
    {
        if (Random.value > spawnChance) return;
        if (spawnablePrefabs.Count == 0) return;
        
        // Clear any existing rewards
        ClearExistingRewards();
        
        // Determine how many rewards to spawn
        int rewardCount = Random.Range(minRewards, maxRewards + 1);
        
        // Create a list of available spawn positions
        List<Transform> availableSpawnPoints = new List<Transform>(spawnPoints);
        
        for (int i = 0; i < rewardCount; i++)
        {
            if (availableSpawnPoints.Count == 0) break;
            if (spawnablePrefabs.Count == 0) break;
            
            // Select random prefab
            int prefabIndex = Random.Range(0, spawnablePrefabs.Count);
            GameObject prefabToSpawn = spawnablePrefabs[prefabIndex];
            
            // Select random spawn point
            int spawnIndex = Random.Range(0, availableSpawnPoints.Count);
            Transform spawnPoint = availableSpawnPoints[spawnIndex];
            availableSpawnPoints.RemoveAt(spawnIndex);
           
            UnityEngine.Vector3 spawnPosition = spawnPoint.position;
            spawnPosition.z = 0f;
            // Create reward
            GameObject reward = Instantiate(prefabToSpawn,spawnPosition,UnityEngine.Quaternion.identity,transform);
            UnityEngine.Vector3 fixedPos = reward.transform.position;
            fixedPos.z = 0f;
            reward.transform.position = fixedPos;
            // Get the Upgrade component
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
            // Play disappear animation
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
                enemy.OnEnemyDeath -= HandleEnemyDeath;
            Debug.Log($"Unsubscribed from enemy death event for: {enemy.name}");
        }
    }
}