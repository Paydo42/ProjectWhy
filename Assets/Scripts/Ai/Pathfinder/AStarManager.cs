// AStarManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for OrderBy

public class AStarManager : MonoBehaviour
{
    // Make this a Singleton for easy access from anywhere
    public static AStarManager Instance { get; private set; }

    // No need to store a single gridGenerator reference here anymore
    // private GridGenerator gridGenerator;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        // No longer finding GridGenerator here
    }

    // Main function to find a path - NOW ACCEPTS a GridGenerator
    public List<Node> FindPath(GridGenerator gridGenerator, Vector3 startPos, Vector3 targetPos)
    {
        // Check if a valid gridGenerator was provided
        if (gridGenerator == null)
        {
            Debug.LogError("A* Pathfinding failed: No GridGenerator provided.", this);
            return null;
        }

        // Use the provided gridGenerator to get nodes
        Node startNode = gridGenerator.GetNodeFromWorldPoint(startPos);
        Node targetNode = gridGenerator.GetNodeFromWorldPoint(targetPos);

        // Basic validation
        if (startNode == null || targetNode == null || startNode.isObstacle || targetNode.isObstacle)
        {
             Debug.LogWarning($"A* Pathfinding failed: Invalid start ({startNode?.name}, obstacle:{startNode?.isObstacle}) or target ({targetNode?.name}, obstacle:{targetNode?.isObstacle}) node in grid {gridGenerator.name}.");
             // Try to find the nearest valid node within the specified grid if target is invalid
             if(targetNode == null || targetNode.isObstacle)
             {
                 Node nearestValidTarget = FindNearestValidNode(gridGenerator, targetPos); // Pass generator
                 if(nearestValidTarget != null) {
                     targetNode = nearestValidTarget;
                     Debug.Log($"Redirecting pathfinding to nearest valid target node: {targetNode.name} in grid {gridGenerator.name}");
                 } else {
                     return null; // Still no valid target found in this grid
                 }
             } else {
                return null; // Start node is invalid
             }
        }

        // A* Algorithm Implementation (remains largely the same)
        List<Node> openSet = new List<Node>();     // Nodes to be evaluated
        HashSet<Node> closedSet = new HashSet<Node>(); // Nodes already evaluated

        openSet.Add(startNode); // Start with the starting node

        // Reset costs from previous pathfinding runs (important!)
        // Find all nodes associated ONLY with the provided grid generator
        Node[] nodesInCurrentGrid = gridGenerator.GetComponentsInChildren<Node>();
        foreach (Node node in nodesInCurrentGrid)
        {
            if (node != null)
            {
                node.gCost = float.MaxValue; // Use MaxValue to ensure first calculation is always lower
                node.hCost = 0;
                node.parentNode = null;
            }
        }
        startNode.gCost = 0; // Cost from start to start is 0
        startNode.hCost = GetDistance(startNode, targetNode); // Initial heuristic

        while (openSet.Count > 0)
        {
            // Find node with the lowest fCost in the open set (simple linear search)
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }
            // More efficient way using Linq (might add slight overhead, but cleaner)
            // Node currentNode = openSet.OrderBy(node => node.fCost).ThenBy(node => node.hCost).First();


            // Move current node from open to closed set
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            // Path found!
            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            // Check neighbours (Neighbours list is pre-calculated by GridGenerator/Node)
            foreach (Node neighbour in currentNode.neighbours)
            {
                // Skip if neighbour is an obstacle or already evaluated
                if (neighbour == null || neighbour.isObstacle || closedSet.Contains(neighbour))
                {
                    continue;
                }

                // Calculate new cost to reach neighbour through current node
                float newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);

                // If new path to neighbour is shorter OR neighbour is not in open set yet
                bool isInOpenSet = openSet.Contains(neighbour);
                if (newMovementCostToNeighbour < neighbour.gCost || !isInOpenSet)
                {
                    // Update costs and parent
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode); // Heuristic cost (distance to target)
                    neighbour.parentNode = currentNode; // Set parent for path retracing

                    // Add neighbour to open set if it's not already there
                    if (!isInOpenSet)
                    {
                        openSet.Add(neighbour);
                    }
                    // No need for an 'update' step if already in openSet,
                    // as the next loop iteration will reconsider it with the new lower fCost.
                }
            }
        }

        // No path found
        Debug.LogWarning($"A* Pathfinding failed: No path found from {startNode.name} to {targetNode.name} in grid {gridGenerator.name}");
        return null;
    }

    // Helper function to retrace the path from end node back to start node
    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        int safetyBreak = 1000; // Prevent infinite loops in case of error
        while (currentNode != startNode && safetyBreak > 0)
        {
            path.Add(currentNode);
            currentNode = currentNode.parentNode; // Move to the parent node
            if (currentNode == null) {
                 Debug.LogError($"Error retracing path: Parent node was null before reaching start node. Target was {endNode.name}", endNode);
                 return null; // Error in path
             }
            safetyBreak--;
        }
         if(safetyBreak <= 0) {
             Debug.LogError($"Error retracing path: Exceeded maximum iterations. Possible loop? Start:{startNode.name}, End:{endNode.name}", endNode);
             return null;
         }

        path.Add(startNode); // Add the start node at the end
        path.Reverse(); // Reverse the list to get path from start to end

        return path;
    }

    // Helper function to calculate the distance between two nodes (heuristic)
    private float GetDistance(Node nodeA, Node nodeB)
    {
        // Simple Euclidean distance (straight line) - works well for grids allowing diagonal movement
         return Vector3.Distance(nodeA.transform.position, nodeB.transform.position);
    }

     // Helper to find the nearest valid node within a SPECIFIC grid
     private Node FindNearestValidNode(GridGenerator gridGenerator, Vector3 worldPosition)
     {
         if (gridGenerator == null) return null;

         Node closestNode = null;
         float minDistanceSq = float.MaxValue;
         // Get nodes associated ONLY with the provided grid generator
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