using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(prefab))
        {
            pools.Add(prefab, new Queue<GameObject>());
        }

        GameObject objectToSpawn;

        if (pools[prefab].Count > 0)
        {
            objectToSpawn = pools[prefab].Dequeue();
        }
        else
        {
            objectToSpawn = Instantiate(prefab);
            // This ensures new objects are parented to the PoolManager for organization
            objectToSpawn.transform.SetParent(this.transform);
        }

        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        return objectToSpawn;
    }

    public void ReturnToPool(GameObject objectToReturn, GameObject originalPrefab)
    {
        if (originalPrefab == null)
        {
            Destroy(objectToReturn);
            return;
        }

        if (!pools.ContainsKey(originalPrefab))
        {
             pools.Add(originalPrefab, new Queue<GameObject>());
        }
        
        objectToReturn.SetActive(false);
        pools[originalPrefab].Enqueue(objectToReturn);
    }
}

