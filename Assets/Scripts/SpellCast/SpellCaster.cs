using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class SpellCaster : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The SpellWordDatabase ScriptableObject with available words")]
    public SpellWordDatabase wordDatabase;

    [Tooltip("The SpellCastUI component (child world-space canvas)")]
    public SpellCastUI spellUI;

    [Header("Settings")]
    [Tooltip("Key to activate a spell")]
    public KeyCode activateKey = KeyCode.Tab;

    [Tooltip("Cooldown between spell casts (seconds)")]
    public float spellCooldown = 5f;

    [Tooltip("Time limit to finish typing the word (0 = no limit)")]
    public float timeLimit = 10f;

    [Tooltip("If true, a wrong letter resets all progress. If false, it just flashes red and waits.")]
    public bool resetOnWrongKey = false;

    [Tooltip("If true, the player cannot move while casting a spell")]
    public bool disableMovementDuringCast = false;

    [Header("Damage Settings")]
    [Tooltip("Fallback damage if the word entry has no damage set")]
    public float defaultSpellDamage = 3f;

    [Tooltip("Visual effect prefab to spawn on each enemy when spell hits (optional)")]
    public GameObject spellHitEffectPrefab;

    // Internal state
    private bool isCasting = false;
    private float lastCastTime = -100f;
    private string currentWord = "";
    private int currentLetterIndex = 0;
    private float castStartTime = 0f;
    private SpellWordDatabase.SpellWord currentSpellWord;

    // Cached references
    private PlayerMovement playerMovement;
    private RoomBounds currentRoom;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        // Activate spell on key press
        if (Input.GetKeyDown(activateKey) && !isCasting && CanCast())
        {
            StartSpell();
        }

        // Handle typing while casting
        if (isCasting)
        {
            HandleTypingInput();

            // Check time limit
            if (timeLimit > 0f && Time.time - castStartTime > timeLimit)
            {
                Debug.Log("Spell casting timed out!");
                CancelSpell();
            }
        }
    }

    /// <summary>
    /// Can the player start a new spell cast?
    /// </summary>
    private bool CanCast()
    {
        if (wordDatabase == null || spellUI == null) return false;
        if (Time.time < lastCastTime + spellCooldown) return false;

        // Find current room - look for the RoomBounds that has active enemies
        currentRoom = FindCurrentRoom();
        if (currentRoom == null) return false;

        return true;
    }

    /// <summary>
    /// Finds the RoomBounds the player is currently in.
    /// </summary>
    private RoomBounds FindCurrentRoom()
    {
        // Use overlap to find which room the player is in
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        foreach (var hit in hits)
        {
            RoomBounds room = hit.GetComponent<RoomBounds>();
            if (room != null)
            {
                return room;
            }
        }
        return null;
    }

    /// <summary>
    /// Begins the spell-casting sequence. Picks a random word and shows it.
    /// </summary>
    private void StartSpell()
    {
        currentSpellWord = wordDatabase.GetRandomWord();
        if (currentSpellWord == null) return;

        currentWord = currentSpellWord.word.ToUpper();
        currentLetterIndex = 0;
        isCasting = true;
        castStartTime = Time.time;

        // Show UI
        spellUI.ShowWord(currentWord);

        // Optionally disable movement
        if (disableMovementDuringCast && playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        Debug.Log($"Spell started! Type: \"{currentWord}\"");
    }

    /// <summary>
    /// Processes keyboard input each frame during casting.
    /// </summary>
    private void HandleTypingInput()
    {
        // Read typed characters this frame from Unity's input string
        foreach (char c in Input.inputString)
        {
            // Ignore non-letter characters
            if (!char.IsLetter(c)) continue;

            char typed = char.ToUpper(c);
            char expected = currentWord[currentLetterIndex];

            if (typed == expected)
            {
                // Correct letter!
                currentLetterIndex++;
                spellUI.AdvanceLetter();

                // Check if word is complete
                if (currentLetterIndex >= currentWord.Length)
                {
                    StartCoroutine(CompleteSpell());
                    return;
                }
            }
            else
            {
                // Wrong letter
                if (resetOnWrongKey)
                {
                    currentLetterIndex = 0;
                    spellUI.ResetProgress();
                    Debug.Log("Wrong key! Progress reset.");
                }
                else
                {
                    spellUI.FlashWrong();
                    Debug.Log($"Wrong key! Expected '{expected}', got '{typed}'");
                }
            }
        }
    }

    /// <summary>
    /// Called when the player finishes typing the word correctly.
    /// Damages all enemies in the current room.
    /// </summary>
    private IEnumerator CompleteSpell()
    {
        isCasting = false;
        lastCastTime = Time.time;

        // Show completion effect
        spellUI.ShowCompleted();
        Debug.Log($"Spell complete! \"{currentWord}\" - Dealing damage to all enemies!");

        // Brief pause for visual feedback
        yield return new WaitForSeconds(0.3f);

        // Deal damage to all enemies in the room
        float damage = currentSpellWord.damage > 0 ? currentSpellWord.damage : defaultSpellDamage;
        DamageAllEnemiesInRoom(damage);

        // Grant time bonus if any
        if (currentSpellWord.timeBonus > 0f)
        {
            GameEvents.TriggerPlayerShot(currentSpellWord.timeBonus);
        }

        // Trigger the game event
        GameEvents.TriggerSpellCast(damage);

        // Brief display then hide
        yield return new WaitForSeconds(0.5f);
        spellUI.Hide();

        // Re-enable movement
        if (disableMovementDuringCast && playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }

    /// <summary>
    /// Cancels the current spell (e.g., on timeout).
    /// </summary>
    private void CancelSpell()
    {
        isCasting = false;
        lastCastTime = Time.time;
        spellUI.Hide();

        // Re-enable movement
        if (disableMovementDuringCast && playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        Debug.Log("Spell cancelled.");
    }

    /// <summary>
    /// Finds all enemies in the current room and deals spell damage to them.
    /// </summary>
    private void DamageAllEnemiesInRoom(float damage)
    {
        if (currentRoom == null)
        {
            // Fallback: damage all enemies with "Enemy" tag in the scene
            DamageAllEnemiesInScene(damage);
            return;
        }

        // Get all Enemy components in the room's children (spawned enemies are parented or tracked by RoomBounds)
        // Since RoomBounds tracks activeEnemies but it's private, we'll find enemies by tag within the room bounds
        Collider2D roomCollider = currentRoom.GetComponent<Collider2D>();
        if (roomCollider == null)
        {
            DamageAllEnemiesInScene(damage);
            return;
        }

        // Find all enemies overlapping the room bounds
        Bounds bounds = roomCollider.bounds;
        Collider2D[] allColliders = Physics2D.OverlapAreaAll(bounds.min, bounds.max);
        
        HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();

        foreach (var col in allColliders)
        {
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null && !damagedEnemies.Contains(enemy))
            {
                damagedEnemies.Add(enemy);
                enemy.TakeDamage(damage);

                // Spawn hit effect if assigned
                if (spellHitEffectPrefab != null)
                {
                    GameObject effect = Instantiate(spellHitEffectPrefab, enemy.transform.position, Quaternion.identity);
                    Destroy(effect, 2f);
                }

                Debug.Log($"Spell dealt {damage} damage to {enemy.name}");
            }
        }

        // Also check EnemyHealth components (for enemies not using the Enemy base class)
        foreach (var col in allColliders)
        {
            if (col.GetComponent<Enemy>() != null) continue; // Already handled above

            EnemyHealth enemyHealth = col.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);

                if (spellHitEffectPrefab != null)
                {
                    GameObject effect = Instantiate(spellHitEffectPrefab, enemyHealth.transform.position, Quaternion.identity);
                    Destroy(effect, 2f);
                }

                Debug.Log($"Spell dealt {damage} damage to {enemyHealth.name}");
            }
        }

        Debug.Log($"Spell hit {damagedEnemies.Count} enemies in room {currentRoom.name}");
    }

    /// <summary>
    /// Fallback: damages all enemies in the scene if room detection fails.
    /// </summary>
    private void DamageAllEnemiesInScene(float damage)
    {
        // Find all Enemy components
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.TakeDamage(damage);
            Debug.Log($"Spell dealt {damage} damage to {enemy.name} (scene-wide fallback)");
        }

        // Also check standalone EnemyHealth components
        EnemyHealth[] enemyHealths = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (var eh in enemyHealths)
        {
            if (eh.GetComponent<Enemy>() == null) // Avoid double damage
            {
                eh.TakeDamage(damage);
            }
        }
    }
}
