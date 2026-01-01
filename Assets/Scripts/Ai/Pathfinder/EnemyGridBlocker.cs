using System.Collections.Generic;
using UnityEngine;

public class EnemyGridBlocker : MonoBehaviour
{
        [SerializeField] private int blockageCost = 20;

        [Header("Blocker Settings")]
        [SerializeField] private float boundsInflation = 0.4f;
        [SerializeField] private float updateInterval = 0.2f;
        private Enemy enemy;
        private Node previousNode;
        private GridGenerator grid;
        private Collider2D _collider;
        private List<Node> blockedNodes = new List<Node>();
        private float timer;
        private void Start()
        {
            enemy = GetComponent<Enemy>();
            _collider = GetComponent<Collider2D>();
             // wait a abit to ensure grid is initialized
            Invoke(nameof(InitBlocker), 0.1f);    
        }
        private void InitBlocker()
    {
        if (enemy != null)
        {
            grid = enemy.currentRoomGridGenerator;

        }
    }
    private void Update()
    {
        if ( grid == null  || enemy == null  || _collider == null) return;

     timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            UpdateBlockage();
            timer = 0f;
        }
    }
    private void UpdateBlockage()
    {
        ClearBlockage();
        // calculate blockage area
        Bounds bounds = _collider.bounds;
        bounds.Expand(boundsInflation);

        List<Node> nodesToBlock = grid.GetNodesOverlappingBounds(bounds);
        foreach (Node node in nodesToBlock)
        {
            if (node != null)
            {
                  node.movementPenalty += blockageCost;
                  blockedNodes.Add(node);   
        }
    }
    }
    private void ClearBlockage()
    {
        foreach (Node node in blockedNodes)
        {
            if (node != null)
            {
                node.movementPenalty -= blockageCost;
                if (node.movementPenalty < 0)
                {
                    node.movementPenalty = 0; // Ensure penalty doesn't go negative
                }
            }
        }
        blockedNodes.Clear();
    }
    private void OnDisable()
    {
        ClearBlockage();
    }
}
