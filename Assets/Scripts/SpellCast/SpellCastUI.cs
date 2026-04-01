using UnityEngine;
using TMPro;

/// <summary>
/// Manages the world-space floating text above the player's head for the spell-typing mechanic.
/// Attach to a child GameObject of the Player that has a Canvas (World Space) + TextMeshProUGUI.
/// </summary>
public class SpellCastUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The TMP text component that displays the word")]
    public TextMeshProUGUI wordText;

    [Header("Colors")]
    public Color untypedColor = Color.white;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public Color completedColor = Color.yellow;

    [Header("Position")]
    [Tooltip("Offset above the player's head")]
    public Vector3 offset = new Vector3(0f, 1.2f, 0f);

    private string currentWord = "";
    private int currentIndex = 0;
    private bool isActive = false;
    private Transform playerTransform;

    void Start()
    {
        // Try to find the player if not already set
        if (playerTransform == null)
        {
            playerTransform = transform.parent;
        }

        Hide();
    }

    void LateUpdate()
    {
        // Keep position above player's head
        if (playerTransform != null && isActive)
        {
            transform.position = playerTransform.position + offset;
        }
    }

    /// <summary>
    /// Shows a new word above the player's head.
    /// </summary>
    public void ShowWord(string word)
    {
        currentWord = word.ToUpper();
        currentIndex = 0;
        isActive = true;

        if (wordText != null)
        {
            gameObject.SetActive(true);
            UpdateDisplay();
        }
    }

    /// <summary>
    /// Call when the player types a correct letter. Advances the highlight.
    /// </summary>
    public void AdvanceLetter()
    {
        currentIndex++;
        UpdateDisplay();
    }

    /// <summary>
    /// Flash the current letter red briefly to indicate a wrong key.
    /// </summary>
    public void FlashWrong()
    {
        if (wordText != null)
        {
            // Rebuild with the current letter in red
            string display = "";
            for (int i = 0; i < currentWord.Length; i++)
            {
                if (i < currentIndex)
                {
                    display += $"<color=#{ColorUtility.ToHtmlStringRGB(correctColor)}>{currentWord[i]}</color>";
                }
                else if (i == currentIndex)
                {
                    display += $"<color=#{ColorUtility.ToHtmlStringRGB(wrongColor)}>{currentWord[i]}</color>";
                }
                else
                {
                    display += $"<color=#{ColorUtility.ToHtmlStringRGB(untypedColor)}>{currentWord[i]}</color>";
                }
            }
            wordText.text = display;
        }
    }

    /// <summary>
    /// Show a completion flash then hide.
    /// </summary>
    public void ShowCompleted()
    {
        if (wordText != null)
        {
            wordText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(completedColor)}>{currentWord}</color>";
        }
    }

    /// <summary>
    /// Hides the spell text UI.
    /// </summary>
    public void Hide()
    {
        isActive = false;
        currentWord = "";
        currentIndex = 0;

        if (wordText != null)
        {
            wordText.text = "";
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Resets progress back to the beginning of the current word (on wrong input, optional).
    /// </summary>
    public void ResetProgress()
    {
        currentIndex = 0;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (wordText == null || string.IsNullOrEmpty(currentWord)) return;

        string display = "";
        for (int i = 0; i < currentWord.Length; i++)
        {
            if (i < currentIndex)
            {
                // Already typed correctly
                display += $"<color=#{ColorUtility.ToHtmlStringRGB(correctColor)}>{currentWord[i]}</color>";
            }
            else
            {
                // Not yet typed
                display += $"<color=#{ColorUtility.ToHtmlStringRGB(untypedColor)}>{currentWord[i]}</color>";
            }
        }

        wordText.text = display;
    }
}
