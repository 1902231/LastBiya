using UnityEngine;

/// <summary>
/// 飞起状态：飞到玩家头顶
/// 到达目标/超过高度上限/碰到墙壁时结束
/// </summary>
public class Boss_FlyUpState : HFSM_BaseState<E_BossStateType_01, Boss01>
{
    private Vector2 targetPosition;
    private bool isComplete;

    public Boss_FlyUpState()
    {
        this.parentType = E_BossStateType_01.Alive;
    }

    public override void OnEnter()
    {
        targetPosition = owner.GetFlyUpTargetPosition();
        if (targetPosition.y > owner.flyUpMaxHeight)
            targetPosition.y = owner.flyUpMaxHeight;
        isComplete = false;
        owner.Rb.gravityScale = 0f;
    }

    public override void OnUpdate()
    {
        if (isComplete) return;

        owner.FacePlayer();

        Vector2 moveDir = (targetPosition - (Vector2)owner.transform.position).normalized;

        if (owner.CheckWallInDirection(moveDir) ||
            owner.transform.position.y >= owner.flyUpMaxHeight ||
            Vector2.Distance(owner.transform.position, targetPosition) <= owner.flyUpArriveDistance)
        {
            Complete();
        }
    }

    public override void OnFixedUpdate()
    {
        if (isComplete) return;

        Vector2 direction = (targetPosition - (Vector2)owner.transform.position).normalized;
        Vector2 targetVelocity = direction * owner.flyUpSpeed;
        Vector2 velocityDiff = targetVelocity - owner.Rb.velocity;
        owner.Rb.AddForce(velocityDiff * owner.moveAcceleration, ForceMode2D.Force);
    }

    public override void OnExit()
    {
        owner.Rb.gravityScale = 1f;
    }

    private void Complete()
    {
        isComplete = true;
        owner.Rb.velocity = Vector2.zero;
        owner.Rb.angularVelocity = 0f;
    }

    public bool HasReachedTarget()
    {
        return isComplete;
    }
}
