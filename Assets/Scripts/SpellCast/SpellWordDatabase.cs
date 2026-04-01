using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SpellWordDatabase", menuName = "SpellCast/Word Database")]
public class SpellWordDatabase : ScriptableObject
{
    [System.Serializable]
    public class SpellWord
    {
        public string word;
        public float damage = 3f;

        [Tooltip("Optional per-word time bonus on successful cast")]
        public float timeBonus = 0f;
    }

    public List<SpellWord> words = new List<SpellWord>();

    /// <summary>
    /// Returns a random word entry from the database.
    /// </summary>
    public SpellWord GetRandomWord()
    {
        if (words == null || words.Count == 0)
        {
            Debug.LogWarning("SpellWordDatabase has no words!");
            return null;
        }
        return words[Random.Range(0, words.Count)];
    }
}
