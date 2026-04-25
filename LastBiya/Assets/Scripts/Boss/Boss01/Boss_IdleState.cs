using UnityEngine;

/// <summary>
/// Idle 状态：待机，类似玩家 Idle
/// </summary>
public class Boss_IdleState : HFSM_BaseState<E_BossStateType_01, Boss01>
{
    public Boss_IdleState()
    {
        this.parentType = E_BossStateType_01.Alive;
    }

    public override void OnEnter()
    {
        // 停止移动
        owner.StopHorizontalMovement();
    }

    public override void OnUpdate()
    {
        // 朝向玩家
        owner.FacePlayer();
    }

    public override void OnFixedUpdate()
    {
        // 保持静止
        owner.StopHorizontalMovement();
    }
}
