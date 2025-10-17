using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{
    public GameObject projectilePrefab;
    public int poolSize = 20;
    private Queue<GameObject> projectilePool = new Queue<GameObject>();

    void Start()
    {
        InitializePool  ();
    }
    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var obj = Instantiate(projectilePrefab);
            obj.SetActive(false);
            projectilePool.Enqueue(obj);
        }
    }
    public GameObject GetObjectFromPool()
    {
        if (projectilePool.Count > 0)
        {
            var obj = projectilePool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            //create new object if pool is empty
            var newObj = Instantiate(projectilePrefab);
            return newObj;
        }
    }
    public void ReturnObjectToPool(GameObject obj)
    {
        obj.SetActive(false);
        projectilePool.Enqueue(obj);
    }
}