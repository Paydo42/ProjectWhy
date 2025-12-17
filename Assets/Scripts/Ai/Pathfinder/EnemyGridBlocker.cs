using UnityEngine;

public class EnemyGridBlocker : MonoBehaviour
{
        [SerializeField] private int blockageCost = 20;
        private Enemy enemy;
        private Node previousNode;
        private GridGenerator grid;

        private void Start()
        {
        enemy = GetComponent<Enemy>();
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
        if ( grid == null  || enemy == null) return;

        Node currentNode = grid.GetNodeFromWorldPoint(transform.position);
        if (currentNode != null && currentNode != previousNode)
        {
            // clear previous node penalty
            if (previousNode != null)
            {
                previousNode.movementPenalty -= blockageCost;
                Debug.Log($"Cleared blockage on node at {previousNode.transform.position} for enemy {enemy.name}");
            }
            // block new node 
            currentNode.movementPenalty += blockageCost;
            Debug.Log($"Set blockage on node at {currentNode.transform.position} for enemy {enemy.name}");
            previousNode = currentNode;
        }
    }
    private void OnDisable()
    {
        //Clear blockage when disabled
        if (previousNode != null)
        {
            previousNode.movementPenalty -= blockageCost;
            previousNode = null;
        }
    }
}
