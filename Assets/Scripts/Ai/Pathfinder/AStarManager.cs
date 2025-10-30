// File: Assets/Scripts/Ai/Pathfinder/AStarManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; 

public class AStarManager : MonoBehaviour
{
    public static AStarManager Instance { get; private set; }

    // --- REMOVED DYNAMIC FIELDS ---
    // We don't need these, as the problem exists without them.
    //[Header("Dynamic Obstacles")]
    //[SerializeField] private LayerMask dynamicObstacleLayer;
    //[SerializeField] private float dynamicObstacleCheckRadius = 0.4f; 
    // --- END REMOVAL ---

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // --- REMOVED IsNodeDynamicallyBlocked ---

    public List<Node> FindPath(GridGenerator gridGenerator, Vector3 startPos, Vector3 targetPos)
    {
        if (gridGenerator == null)
        {
            Debug.LogError("A* Pathfinding failed: No GridGenerator provided.", this);
            return null;
        }

        Node startNode = gridGenerator.GetNodeFromWorldPoint(startPos);
        Node targetNode = gridGenerator.GetNodeFromWorldPoint(targetPos);

        // Basic validation
        if (startNode == null || startNode.isObstacle)
        {
             startNode = FindNearestValidNode(gridGenerator, startPos); // Find nearest valid
             if (startNode == null)
             {
                Debug.LogWarning($"A* Pathfinding failed: Start position {startPos} is blocked or invalid.");
                return null; // No valid start node
             }
        }
        if (targetNode == null || targetNode.isObstacle)
        {
            Node nearestValidTarget = FindNearestValidNode(gridGenerator, targetPos); // Find nearest valid
            if (nearestValidTarget != null)
            {
                targetNode = nearestValidTarget;
            }
            else
            {
                Debug.LogWarning($"A* Pathfinding failed: No valid target node found near {targetPos} in grid {gridGenerator.name}.");
                return null; // Still no valid target found
            }
        }
        
        // This check is fine to remove, RetracePath will handle it.
        // if (startNode == targetNode) ...

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        Node[] nodesInCurrentGrid = gridGenerator.GetComponentsInChildren<Node>();
        foreach (Node node in nodesInCurrentGrid)
        {
            if (node != null)
            {
                node.gCost = float.MaxValue;
                node.hCost = 0;
                node.parentNode = null;
            }
        }
        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (Node neighbour in currentNode.neighbours)
            {
                // --- Reverted to the simpler check ---
                if (neighbour == null || neighbour.isObstacle || closedSet.Contains(neighbour))
                {
                    continue;
                }
                // --- END REVERT ---

                float newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                bool isInOpenSet = openSet.Contains(neighbour);

                if (newMovementCostToNeighbour < neighbour.gCost || !isInOpenSet)
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parentNode = currentNode;

                    if (!isInOpenSet)
                    {
                        openSet.Add(neighbour);
                    }
                }
            }
        }

        Debug.LogWarning($"A* Pathfinding failed: No path found from {startNode.name} to {targetNode.name} in grid {gridGenerator.name}");
        return null;
    }

    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        int safetyBreak = 1000;
        while (currentNode != startNode && safetyBreak > 0)
        {
            path.Add(currentNode);
            currentNode = currentNode.parentNode;
            if (currentNode == null) {
                Debug.LogError($"Error retracing path: Parent node was null before reaching start node. Target was {endNode.name}", endNode);
                return null;
            }
            safetyBreak--;
        }
        if(safetyBreak <= 0) {
            Debug.LogError($"Error retracing path: Exceeded maximum iterations. Possible loop? Start:{startNode.name}, End:{endNode.name}", endNode);
            return null;
        }

        // --- FIX 1 ---
        // Put this line back. This guarantees we never return an empty list.
        path.Add(startNode); 
        // --- END FIX ---

        path.Reverse();
        return path;
    }
    private float GetDistance(Node nodeA, Node nodeB)
    {
        return Vector3.Distance(nodeA.transform.position, nodeB.transform.position);
    }

    // --- REVERTED HELPER METHOD ---
    // We revert to the simpler version without dynamic checks.
    private Node FindNearestValidNode(GridGenerator gridGenerator, Vector3 worldPosition)
    {
        if (gridGenerator == null) return null;

        Node closestNode = null;
        float minDistanceSq = float.MaxValue;
        Node[] allNodes = gridGenerator.GetComponentsInChildren<Node>();

        foreach (Node node in allNodes)
        {
            if (node != null && !node.isObstacle)
            {
                float distSq = (node.transform.position - worldPosition).sqrMagnitude;
                if (distSq < minDistanceSq)
                {
                    minDistanceSq = distSq;
                    closestNode = node;
                }
            }
        }
        return closestNode;
    }
}