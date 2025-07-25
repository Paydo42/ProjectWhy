using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HealthUiManager : MonoBehaviour
{
    public GameObject heartPrefab;
    private PlayerHealth playerHealth;
    List<Image> hearts = new List<Image>();

    public void Initialize(PlayerHealth health)
    {
        Debug.Log("HealthUI Initializing");
        playerHealth = health;
        DrawHearts();
    }

    public void DrawHearts()
    {
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth reference is missing!");
            return;
        }
        
        Debug.Log($"Drawing hearts - Current: {playerHealth.currentHealth}, Max: {playerHealth.maxHealth}");

        // Clear existing hearts only if max health changed
        int requiredHearts = Mathf.CeilToInt(playerHealth.maxHealth / 2f);
        if (hearts.Count != requiredHearts)
        {
            ClearHearts();
            for (int i = 0; i < requiredHearts; i++)
            {
                CreateHeartContainer();
            }
        }

        // Update hearts based on current health
        float remainingHealth = playerHealth.currentHealth;
        
        for (int i = 0; i < hearts.Count; i++)
        {
            if (remainingHealth <= 0)
            {
                hearts[i].sprite = playerHealth.emptyHeart;
            }
            else if (remainingHealth >= 2)
            {
                hearts[i].sprite = playerHealth.fullHeart;
                remainingHealth -= 2;
            }
            else
            {
                hearts[i].sprite = playerHealth.halfHeart;
                remainingHealth -= 1;
            }
        }
    }

    void CreateHeartContainer()
    {
        GameObject newHeart = Instantiate(heartPrefab, transform);
        hearts.Add(newHeart.GetComponent<Image>());
    }

    void ClearHearts()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        hearts.Clear();
    }
}