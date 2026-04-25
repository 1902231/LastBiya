using UnityEngine;

/// <summary>
/// 巡逻状态：在平台上左右移动，碰到边缘掉头，发现玩家切追逐
/// </summary>
public class Enemy_01_PatrolState : HFSM_BaseState<E_Enemy01StateType, Enemy_01>
{
    public Enemy_01_PatrolState()
    {
        this.parentType = E_Enemy01StateType.Alive;
    }

    public override void OnEnter()
    {
        // 以当前朝向开始巡逻
    }

    public override void OnUpdate()
    {
        // 发现玩家 → 追逐
        if (owner.DetectPlayer())
        {
            hfsm.SwitchState(E_Enemy01StateType.Chase);
            return;
        }

        // 前方没有平台 或 前方有墙壁 → 掉头
        if (!owner.HasGroundAhead(owner.FacingDirection) || owner.HasWallAhead(owner.FacingDirection))
        {
            owner.UpdateFacing(-owner.FacingDirection);
        }
    }

    public override void OnFixedUpdate()
    {
        owner.Rb.velocity = new Vector2(owner.FacingDirection * owner.patrolSpeed, owner.Rb.velocity.y);
    }
}
