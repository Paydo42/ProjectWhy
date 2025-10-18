using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemyMoveable
{
    Rigidbody2D RB { get; }
    bool IsFacingRight { get; set; }
    void MoveEnemy(UnityEngine.Vector2 velocity);
    void CheckForLeftOrRightFacing(UnityEngine.Vector2 velocity);

}
