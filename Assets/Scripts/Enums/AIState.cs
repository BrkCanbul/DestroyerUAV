using UnityEngine;

public enum AIState
{
    PATROL,
    CHASE,
    RETURN_TO_BASE,
    DEAD
}

public  enum AIStateTransition
{
    SEE_PLAYER,
    LOSE_PLAYER,
    REACH_PATROL_POINT,
    LOW_HEALTH,
    REACHED_BASE,
    DIED

}