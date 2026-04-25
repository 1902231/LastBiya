using UnityEngine;

/// <summary>
/// 跟踪状态：持续移向玩家上方锚点，计时结束后发起俯冲
/// </summary>
public class Enemy_03_TrackingState : HFSM_BaseState<E_Enemy03StateType, Enemy_03>
{
    private float timer;

    public Enemy_03_TrackingState()
    {
        this.parentType = E_Enemy03StateType.Alive;
    }

    public override void OnEnter()
    {
        timer = 0f;
    }

    public override void OnUpdate()
    {
        // 使用追击检测范围（更大，避免频繁切换）
        if (!owner.DetectPlayer(owner.trackingDetectRange))
        {
            hfsm.SwitchState(E_Enemy03StateType.Patrol);
            return;
        }

        // 朝向玩家
        owner.UpdateFacing(owner.PlayerTransform.position.x);

        timer += Time.deltaTime;
        if (timer >= owner.trackingDuration)
        {
            hfsm.SwitchState(E_Enemy03StateType.DiveAttack);
            return;
        }
    }

    public override void OnFixedUpdate()
    {
        // 移向距离更近的锚点
        Vector2 anchor = owner.GetCloserAnchorPoint();
        Vector2 dir = (anchor - (Vector2)owner.transform.position).normalized;
        owner.Rb.velocity = dir * owner.trackingSpeed;
    }
}
