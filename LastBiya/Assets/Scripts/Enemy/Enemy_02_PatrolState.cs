using UnityEngine;

public class Enemy_02_PatrolState : HFSM_BaseState<E_Enemy02StateType, Enemy_02>
{
    public Enemy_02_PatrolState()
    {
        this.parentType = E_Enemy02StateType.Alive;
    }

    public override void OnEnter()
    {
        // 以当前朝向开始巡逻
    }

    public override void OnUpdate()
    {
        // 前方没有平台 或 前方有墙壁 → 掉头
        if (!owner.HasGroundAhead(owner.FacingDirection) || owner.HasWallAhead(owner.FacingDirection))
        {
            owner.UpdateFacing(-owner.FacingDirection);
            return;
        }
    }

    public override void OnFixedUpdate()
    {
        owner.Rb.velocity = new Vector2(owner.FacingDirection * owner.moveSpeed, owner.Rb.velocity.y);
    }
}
