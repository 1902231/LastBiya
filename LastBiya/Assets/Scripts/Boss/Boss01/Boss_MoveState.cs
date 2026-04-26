using UnityEngine;

/// <summary>
/// Move 状态：向玩家方向移动
/// </summary>
public class Boss_MoveState : HFSM_BaseState<E_BossStateType_01, Boss01>
{
    public Boss_MoveState()
    {
        this.parentType = E_BossStateType_01.Alive;
    }

    public override void OnUpdate()
    {
        // 朝向玩家
        owner.FacePlayer();
    }

    public override void OnFixedUpdate()
    {
        if (owner.PlayerTransform == null) return;

        // 向玩家方向移动
        float dirToPlayer = owner.PlayerTransform.position.x > owner.transform.position.x ? 1 : -1;
        owner.ApplyHorizontalMovement(dirToPlayer);
    }
}
