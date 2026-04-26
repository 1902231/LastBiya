using UnityEngine;

/// <summary>
/// 破防倒地状态
/// 进入时被击退到斜上方，落地后开始计时
/// 计时期间韧性持续恢复，计时结束时韧性正好回满
/// </summary>
public class Boss_StunnedState : HFSM_BaseState<E_BossStateType_01, Boss01>
{
    private float timer;
    private bool hasLanded;

    public Boss_StunnedState()
    {
        this.parentType = E_BossStateType_01.Alive;
    }

    public override void OnEnter()
    {
        timer = 0f;
        hasLanded = false;

        // 清零速度，施加斜上方击退力
        owner.Rb.velocity = Vector2.zero;

        float rad = owner.stunnedKnockbackAngle * Mathf.Deg2Rad;
        // 击退方向：远离玩家的斜上方
        int knockbackDir = owner.PlayerTransform != null
            ? (owner.transform.position.x > owner.PlayerTransform.position.x ? 1 : -1)
            : -owner.FacingDirection;

        Vector2 force = new Vector2(
            knockbackDir * Mathf.Sin(rad),
            Mathf.Cos(rad)
        ).normalized * owner.stunnedKnockbackForce;

        owner.Rb.AddForce(force, ForceMode2D.Impulse);
    }

    public override void OnUpdate()
    {
        // 等待落地
        if (!hasLanded)
        {
            if (owner.IsGrounded)
            {
                hasLanded = true;
                owner.Rb.velocity = Vector2.zero;
            }
            return;
        }

        // 落地后开始计时
        timer += Time.deltaTime;

        // 韧性持续恢复：根据计时时长线性回满
        float recoveryRate = owner.maxPosture / owner.stunnedDuration;
        owner.currentPosture = Mathf.Min(
            owner.currentPosture + recoveryRate * Time.deltaTime,
            owner.maxPosture
        );

        // 计时结束，强制韧性回满
        if (timer >= owner.stunnedDuration)
            owner.currentPosture = owner.maxPosture;
    }

    public override void OnFixedUpdate()
    {
        // 落地后保持静止
        if (hasLanded)
            owner.StopHorizontalMovement();
    }

    /// <summary>
    /// 检查破防是否结束
    /// </summary>
    public bool IsStunComplete()
    {
        return hasLanded && timer >= owner.stunnedDuration && owner.currentPosture >= owner.maxPosture;
    }
}
