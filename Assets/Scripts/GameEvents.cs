// GameEvents.cs
using UnityEngine;

public static class GameEvents
{
    public delegate void SpellCastHandler(float damage);

    public static event SpellCastHandler OnSpellCast;

    public static void TriggerSpellCast(float damage)
    {
        OnSpellCast?.Invoke(damage);
    }

}