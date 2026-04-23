using UnityEngine;

/// <summary>
/// 追逐状态：朝玩家方向移动，不离开平台，进入攻击范围切攻击，丢失目标切巡逻
/// </summary>
public class Enemy_01_ChaseState : HFSM_BaseState<E_Enemy01StateType, Enemy_01>
{
    public Enemy_01_ChaseState()
    {
        this.parentType = E_Enemy01StateType.Alive;
    }

    public override void OnUpdate()
    {
        // 丢失目标 → 回巡逻
        if (!owner.DetectPlayer())
        {
            hfsm.SwitchState(E_Enemy01StateType.Patrol);
            return;
        }

        // 进入攻击范围且 CD 好了 → 攻击
        if (owner.IsInAttackRange() && owner.attackCooldownTimer <= 0)
        {
            hfsm.SwitchState(E_Enemy01StateType.Attack);
            return;
        }

        // 朝玩家方向移动
        int dirToPlayer = owner.PlayerTransform.position.x > owner.transform.position.x ? 1 : -1;
        owner.UpdateFacing(dirToPlayer);

        // 前方没有平台 → 停下，不追了
        if (!owner.HasGroundAhead(dirToPlayer))
        {
            owner.Rb.velocity = new Vector2(0, owner.Rb.velocity.y);
            return;
        }
    }

    public override void OnFixedUpdate()
    {
        // 只有前方有平台才移动
        if (owner.HasGroundAhead(owner.FacingDirection))
        {
            owner.Rb.velocity = new Vector2(owner.FacingDirection * owner.chaseSpeed, owner.Rb.velocity.y);
        }
    }
}
