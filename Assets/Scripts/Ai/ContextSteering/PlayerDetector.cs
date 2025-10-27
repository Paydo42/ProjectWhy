// Create file: Assets/Scripts/AI/ContextSteering/PlayerDetector.cs
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AIData))]
public class PlayerDetector : Detector
{
    private AIData aiData;

    private void Awake()
    {
        aiData = GetComponent<AIData>();
    }

    public override void Detect(AIData _)
    {
        // Use your Player Singleton
        if (Player.Instance != null)
        {
            if (aiData.targets == null || aiData.targets.Count == 0)
            {
                // If list is empty, create it and add the player
                aiData.targets = new List<Transform> { Player.Instance.transform };
            }
            else
            {
                // Otherwise, just make sure the first target is the player
                aiData.targets[0] = Player.Instance.transform;
            }
        }
        else
        {
            // No player found, clear the targets
            if (aiData.targets != null)
            {
                aiData.targets.Clear();
            }
        }
    }
}