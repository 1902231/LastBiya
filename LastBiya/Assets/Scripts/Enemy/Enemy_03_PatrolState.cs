using UnityEngine;

/// <summary>
/// 巡逻状态：在出生点附近随机漂移，发现玩家切跟踪
/// </summary>
public class Enemy_03_PatrolState : HFSM_BaseState<E_Enemy03StateType, Enemy_03>
{
    private Vector2 driftTarget;
    private Vector2 originPos;
    private float changeTimer;
    private float changInterval = 2f;
    private float driftRadius = 2f;

    public Enemy_03_PatrolState()
    {
        this.parentType = E_Enemy03StateType.Alive;
    }

    public override void OnEnter()
    {
        originPos = owner.transform.position;
        PickNewDriftTarget();
    }

    public override void OnUpdate()
    {
        // 使用巡逻检测范围
        if (owner.DetectPlayer(owner.patrolDetectRange))
        {
            hfsm.SwitchState(E_Enemy03StateType.Tracking);
            return;
        }

        changeTimer += Time.deltaTime;
        if (changeTimer >= changInterval)
            PickNewDriftTarget();
    }

    public override void OnFixedUpdate()
    {
        Vector2 dir = (driftTarget - (Vector2)owner.transform.position).normalized;
        owner.Rb.velocity = dir * owner.patrolSpeed;
    }

    private void PickNewDriftTarget()
    {
        changeTimer = 0f;
        driftTarget = originPos + Random.insideUnitCircle * driftRadius;
    }
}
