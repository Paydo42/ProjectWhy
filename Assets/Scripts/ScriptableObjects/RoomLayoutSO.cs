using UnityEngine;
using System.Collections.Generic;

// This struct will hold the data for a single enemy spawn
[System.Serializable]
public struct EnemyPlacement
{
    public GameObject enemyPrefab; // The enemy prefab to spawn (e.g., Angel, Bouncer)
    public Vector2 position;       // The local position within the room to spawn it
}

// This makes a new option in the "Create" menu to create these layout files
[CreateAssetMenu(fileName = "New Room Layout", menuName = "Dungeon/Room Layout")]
public class RoomLayout : ScriptableObject
{
    // A list of all enemies to spawn for this specific room layout
    public List<EnemyPlacement> enemiesToSpawn;
}
