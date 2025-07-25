using UnityEngine;

public interface ITriggerCheckAble
{
    bool IsAggroed { get; set; }
    bool IsWithInAttackDistance { get; set; }
    void SetAggroStatus (bool isAggroed);
    void SetAttackDistanceStatus (bool isWithInAttackDistance);

}  
