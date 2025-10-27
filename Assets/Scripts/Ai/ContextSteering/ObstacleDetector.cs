// Create file: Assets/Scripts/AI/ContextSteering/ObstacleDetector.cs
using UnityEngine;
using System.Collections.Generic;
[RequireComponent(typeof(AIData))]
public class ObstacleDetector : Detector
{
    [SerializeField]
    private float detectRadius = 5f;

    [SerializeField]
    private LayerMask obstacleLayer;

    [SerializeField]
    private bool showGizmos = true;

    private AIData aiData;
    private List<Detector> detectors;

    private void Awake()
    {
        aiData = GetComponent<AIData>();
        
        // This is a bit of a workaround to make sure Detect() is called.
        // A better system would have a central AI brain call Detect() on all detectors.
        // For now, this will work.
        detectors = new List<Detector>(GetComponents<Detector>());
    }

    // We call Detect from Update()
    private void Update()
    {
        foreach (var detector in detectors)
        {
            detector.Detect(aiData);
        }
    }

    public override void Detect(AIData _)
    {
        // Find all colliders on the 'obstacleLayer' within a 'detectRadius'
        aiData.obstacles = Physics2D.OverlapCircleAll(transform.position, detectRadius, obstacleLayer);
    }

    private void OnDrawGizmos()
    {
        if (showGizmos)
        {
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, detectRadius);
        }
    }
}