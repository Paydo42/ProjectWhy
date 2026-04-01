// GameEvents.cs
using UnityEngine;

public static class GameEvents
{
    public delegate void TimeRewardHandler(float seconds);
    public delegate void SpellCastHandler(float damage);

    public static event TimeRewardHandler OnEnemyKilled;
    public static event TimeRewardHandler OnPlayerShot;
    public static event SpellCastHandler OnSpellCast;

    public static void TriggerSpellCast(float damage)
    {
        OnSpellCast?.Invoke(damage);
    }

    public static void TriggerEnemyKilled(float seconds)
    {
        OnEnemyKilled?.Invoke(seconds);
        // if(OnEnemyKilled != null) 
        //{
        // OnEnemyKilled(seconds); // Direct call instead of Invoke
        //}
    }
    public static void TriggerPlayerShot(float seconds)
    {
        Debug.Log($"Player shot event triggered with {seconds} seconds reward.");
        OnPlayerShot?.Invoke(seconds);
        // if(OnPlayerShot != null) 
        //{
        // OnPlayerShot(seconds); // Direct call instead of Invoke
        //}
    }
}