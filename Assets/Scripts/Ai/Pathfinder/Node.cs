using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    // A* Variables
    [HideInInspector] public float gCost, hCost; // gCost = distance from start node, hCost = distance to end node
    [HideInInspector] public Node parentNode; // Node preceding this one in the path

    // Neighbours
    public List<Node> neighbours = new List<Node>(); // List of directly reachable neighbour nodes

    // Obstacle Check
    public bool isObstacle = false; // Is this node blocked?
    public LayerMask obstacleLayer; // Layer mask to detect obstacles
    private float checkRadius = 0.4f; // Radius to check for obstacles around the node

    private void Start()
    {
        // Automatically check if the node is obstructed on start
        CheckIfObstacle();
    }

    // A* Calculation: fCost = gCost + hCost
    public float fCost
    {
        get { return gCost + hCost; }
    }

    // Method to find and store neighbours
    public void FindNeighbours(float threshold)
    {
        neighbours.Clear(); // Clear previous neighbours before finding new ones

        // --- UPDATED LINE ---
        Node[] allNodes = FindObjectsByType<Node>(FindObjectsSortMode.None); // Find all nodes in the scene using the newer method
        // --- END UPDATE ---

        foreach (Node node in allNodes)
        {
            if (node == this) continue; // Skip self

            // Check distance to see if it's a potential neighbour
            if (Vector2.Distance(node.transform.position, transform.position) <= threshold)
            {
                // Raycast to ensure there's a clear path between the nodes
                RaycastHit2D hit = Physics2D.Linecast(transform.position, node.transform.position, obstacleLayer);
                if (hit.collider == null) // No obstacle hit
                {
                    neighbours.Add(node); // Add as a neighbour
                }
            }
        }
    }

    // Method to check if the node itself is obstructed
    public void CheckIfObstacle()
    {
        Collider2D collision = Physics2D.OverlapCircle(transform.position, checkRadius, obstacleLayer);
        isObstacle = collision != null; // If collision detected, mark as obstacle
    }

    // --- Gizmos for visualization in the Editor ---
    private void OnDrawGizmos()
    {
        // Draw the node itself
        Gizmos.color = isObstacle ? Color.red : Color.green; // Red if obstacle, green otherwise
        Gizmos.DrawSphere(transform.position, 0.2f); // Draw a small sphere at the node's position

        // Draw lines to neighbours
        if (neighbours.Count > 0)
        {
            Gizmos.color = Color.yellow; // Yellow lines for connections
            foreach (Node neighbour in neighbours)
            {
                if (neighbour != null)
                {
                    Gizmos.DrawLine(transform.position, neighbour.transform.position);
                }
            }
        }
    }

    // Optional: Draw only when selected for less clutter
    private void OnDrawGizmosSelected()
    {
        // Draw check radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, checkRadius); // Show obstacle check radius
    }
    // --- End Gizmos ---
}