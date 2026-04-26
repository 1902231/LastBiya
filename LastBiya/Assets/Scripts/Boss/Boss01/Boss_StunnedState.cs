using UnityEngine;

/// <summary>
/// 破防倒地状态：Boss 无法行动
/// </summary>
public class Boss_StunnedState : HFSM_BaseState<E_BossStateType_01, Boss01>
{
    private float timer;

    public Boss_StunnedState()
    {
        this.parentType = E_BossStateType_01.Alive;
    }

    public override void OnEnter()
    {
        timer = 0f;
        
        // 停止移动
        owner.StopHorizontalMovement();
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;

        if (timer >= owner.stunnedDuration)
        {
            // 破防结束，由 Behavior Designer 决定下一步
        }
    }

    public override void OnFixedUpdate()
    {
        // 保持静止
        owner.StopHorizontalMovement();
    }

    /// <summary>
    /// 检查破防是否结束（供 Behavior Designer 查询）
    /// </summary>
    public bool IsStunComplete()
    {
        return timer >= owner.stunnedDuration;
    }
}
