using UnityEngine;

public class Player : MonoBehaviour
{
    // This is the static instance that all other scripts can access
    public static Player Instance { get; private set; }

    void Awake()
    {
        // This sets up the singleton pattern
        if (Instance == null)
        {
            // If no instance exists, this one becomes the instance
            Instance = this;

            // Optional: Use this if your player needs to persist between scenes
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            // If an instance already exists, destroy this duplicate
            Destroy(gameObject);
        }
    }
}