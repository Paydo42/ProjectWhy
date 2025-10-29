// GridGenerator.cs
using UnityEngine;
using System.Collections.Generic;

// Require a Collider2D component to ensure we have bounds information
[RequireComponent(typeof(BoxCollider2D))] // Or PolygonCollider2D, depending on your RoomBounds
public class GridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    // We'll calculate size from the collider now, so we hide gridWorldSize
    // [SerializeField] private Vector2 gridWorldSize = new Vector2(20, 20);
    [SerializeField] private float nodeRadius = 0.5f; // Radius each node covers
    [SerializeField] private LayerMask obstacleLayer; // Layer(s) containing obstacles
    [SerializeField] private float nodeConnectionThreshold = 1.5f; // Distance threshold for finding neighbours (relative to diameter)
    [SerializeField, Tooltip("Optional padding inside the bounds to avoid placing nodes too close to walls.")]
    private float boundsPadding = 0.1f; // Add padding

    [Header("Prefab")]
    [SerializeField] private GameObject nodePrefab; // Prefab to use for creating nodes

    // Reference to the room bounds collider
    [Header("Room Definition")]
    [SerializeField, Tooltip("Assign the Collider2D defining the room boundaries here.")]
    private Collider2D roomBoundsCollider; // Reference to the RoomBounds collider

    private Node[,] grid; // 2D array to store the generated nodes
    private float nodeDiameter;
    private int gridSizeX, gridSizeY;
    private Vector2 actualGridSize; // Store the size derived from the collider
    private Vector3 gridOrigin; // Store the calculated origin (bottom-left)

     private void Awake()
     {
         // Get the collider if not assigned, assuming it's on the same GameObject
         if (roomBoundsCollider == null)
         {
             roomBoundsCollider = GetComponent<Collider2D>();
         }

         if (roomBoundsCollider == null)
         {
              Debug.LogError("GridGenerator: No Room Bounds Collider assigned or found on this GameObject!", this);
         }
     }

    // Context menu function to create the grid in the editor
    [ContextMenu("Create Grid from Bounds")]
    public void CreateGrid()
    {
        if (nodePrefab == null) {
            Debug.LogError("Node Prefab is not assigned!", this);
            return;
        }
        if (roomBoundsCollider == null) {
            Debug.LogError("Room Bounds Collider is not assigned!", this);
             // Try to get it again, useful if assigning just before clicking
             roomBoundsCollider = GetComponent<Collider2D>();
             if (roomBoundsCollider == null) return;
        }

        ClearGrid(); // Clear any existing grid first

        nodeDiameter = nodeRadius * 2;

        // --- Use Collider Bounds ---
        Bounds bounds = roomBoundsCollider.bounds;
        // Apply padding to shrink the grid area slightly
        actualGridSize = new Vector2(bounds.size.x - (boundsPadding * 2), bounds.size.y - (boundsPadding * 2));
        gridOrigin = bounds.center - (Vector3)actualGridSize / 2; // Calculate bottom-left based on center and padded size
        // --- End Use Collider Bounds ---

        // Calculate how many nodes fit into the grid size
        gridSizeX = Mathf.Max(1, Mathf.RoundToInt(actualGridSize.x / nodeDiameter)); // Ensure at least 1 node
        gridSizeY = Mathf.Max(1, Mathf.RoundToInt(actualGridSize.y / nodeDiameter));

        grid = new Node[gridSizeX, gridSizeY];

        // Instantiate and position the nodes
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                // Calculate the world position for the current node, offset from the calculated origin
                Vector3 worldPoint = gridOrigin + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.up * (y * nodeDiameter + nodeRadius);

                 // Check if the node position is actually inside the collider bounds (important for non-rectangular colliders)
                 if (!roomBoundsCollider.OverlapPoint(worldPoint))
                 {
                     continue; // Skip node creation if it's outside the precise bounds
                 }

                // Instantiate the node prefab
                GameObject nodeGO = Instantiate(nodePrefab, worldPoint, Quaternion.identity, transform); // Parent to GridGenerator
                nodeGO.name = $"Node_{x}_{y}";
                Node newNode = nodeGO.GetComponent<Node>();

                if (newNode != null)
                {
                    newNode.obstacleLayer = obstacleLayer; // Assign the obstacle layer to the Node script
                    newNode.CheckIfObstacle(); // Perform obstacle check
                    grid[x, y] = newNode; // Store the node in the grid array
                }
                else
                {
                    Debug.LogError($"Node script not found on instantiated object: {nodeGO.name}", nodeGO);
                    DestroyImmediate(nodeGO); // Destroy the problematic object immediately in editor
                }
            }
        }

        // Assign neighbours after all nodes are created
        AssignNeighbours();

        Debug.Log($"Grid created from bounds with dimensions: {gridSizeX}x{gridSizeY}.");
    }

    // Context menu function to clear the grid in the editor
    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        // Find all Node objects that are children of this GridGenerator and destroy them
        Node[] existingNodes = GetComponentsInChildren<Node>();
        for (int i = existingNodes.Length - 1; i >= 0; i--) // Iterate backwards when destroying
        {
            if (existingNodes[i] != null && existingNodes[i].gameObject != null)
            {
                // Use DestroyImmediate when running in the editor outside play mode
                if (Application.isEditor && !Application.isPlaying)
                    DestroyImmediate(existingNodes[i].gameObject);
                else // Use Destroy during play mode
                    Destroy(existingNodes[i].gameObject);
            }
        }

        grid = null; // Reset the grid array
        Debug.Log("Existing grid cleared.");
    }


    // Function to find and assign neighbours for all nodes
    private void AssignNeighbours()
    {
         Node[] nodesInGrid = GetComponentsInChildren<Node>(); // Get only the nodes created by this generator

         if (nodesInGrid == null || nodesInGrid.Length == 0) return;

         float thresholdDistanceSq = Mathf.Pow(nodeConnectionThreshold * nodeDiameter, 2); // Use squared distance for efficiency

         foreach (Node node in nodesInGrid)
         {
             if (node == null) continue;
             node.neighbours.Clear(); // Clear existing neighbours first

             foreach (Node potentialNeighbour in nodesInGrid)
             {
                 if (node == potentialNeighbour || potentialNeighbour == null) continue; // Skip self and nulls

                 // Check distance (squared)
                 if ((potentialNeighbour.transform.position - node.transform.position).sqrMagnitude <= thresholdDistanceSq)
                 {
                     // Raycast to ensure there's a clear path between the nodes
                     RaycastHit2D hit = Physics2D.Linecast(node.transform.position, potentialNeighbour.transform.position, obstacleLayer);
                     if (hit.collider == null) // No obstacle hit
                     {
                         node.neighbours.Add(potentialNeighbour); // Add as a neighbour
                     }
                 }
             }
         }
         Debug.Log("Neighbour connections assigned.");
    }


    // Draw gizmos in the editor to visualize the grid area based on the collider
    void OnDrawGizmosSelected()
    {
         if (roomBoundsCollider != null)
         {
             Bounds bounds = roomBoundsCollider.bounds;
             Vector2 paddedSize = new Vector2(bounds.size.x - (boundsPadding * 2), bounds.size.y - (boundsPadding * 2));
             Gizmos.color = new Color(0, 1, 0, 0.5f); // Semi-transparent green
             Gizmos.DrawWireCube(bounds.center, paddedSize); // Draw wireframe based on padded bounds
         }
         else // Fallback if collider isn't set yet
         {
             Gizmos.color = new Color(1, 0, 0, 0.3f); // Red tint to indicate collider missing
             Gizmos.DrawWireCube(transform.position, actualGridSize); // Use last calculated size if available
         }

         // The Node gizmos will draw the individual nodes and connections
    }


    // Public method to get the closest valid node to a world position
    public Node GetNodeFromWorldPoint(Vector3 worldPosition)
    {
         if (grid == null || gridSizeX == 0 || gridSizeY == 0)
         {
             Debug.LogWarning("Grid not initialized or has zero size.");
             return null;
         }

         // Convert world position to a percentage relative to the *actual* grid size and origin
         float percentX = Mathf.Clamp01((worldPosition.x - gridOrigin.x) / actualGridSize.x);
         float percentY = Mathf.Clamp01((worldPosition.y - gridOrigin.y) / actualGridSize.y);


         // Convert percentage to grid coordinates
         int x = Mathf.Clamp(Mathf.FloorToInt(gridSizeX * percentX), 0, gridSizeX - 1);
         int y = Mathf.Clamp(Mathf.FloorToInt(gridSizeY * percentY), 0, gridSizeY - 1);


         // Important: Check if the calculated grid index actually contains a node
         // (it might be null if it was outside the collider bounds during generation)
         Node potentialNode = grid[x, y];

         // If the direct node is null or an obstacle, we might want to search nearby,
         // but for simplicity now, just return null if it's invalid.
         if(potentialNode == null || potentialNode.isObstacle)
         {
             // Optional: Implement logic here to find the nearest *valid* node if needed.
             // For now, returning null indicates the point isn't directly on a valid node.
              // Debug.LogWarning($"Point {worldPosition} maps to invalid grid cell [{x},{y}]");
             return FindNearestValidNode(worldPosition); // Let's add a helper for this
         }

         return potentialNode;
    }

     // Helper to find the nearest non-obstacle node if the target point maps to an invalid one
     private Node FindNearestValidNode(Vector3 worldPosition)
     {
         Node closestNode = null;
         float minDistanceSq = float.MaxValue;
         Node[] allNodes = GetComponentsInChildren<Node>(); // Get nodes managed by this grid

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