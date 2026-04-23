using UnityEngine;

public class PlayerState_Heart : HFSM_BaseState<E_PlayerStateType, PlayerController>
{
    private float timer;
    private bool hasRecovered;

    public PlayerState_Heart()
    {
        this.parentType = E_PlayerStateType.Alive;
    }

    public override void OnEnter()
    {
        timer = 0f;
        hasRecovered = false;

        // 顿帧：冻结时间
        Time.timeScale = owner.HurtTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 冻结玩家速度
        owner.Rb.velocity = Vector2.zero;
    }

    public override void OnUpdate()
    {
        // 阶段一：顿帧，用 unscaledDeltaTime 计时
        if (!hasRecovered)
        {
            timer += Time.unscaledDeltaTime;
            if (timer >= owner.HitStopDuration)
            {
                hasRecovered = true;
                timer = 0f;

                // 恢复时间
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;

                // 施加击退力（水平 + 向上抛起）
                var info = owner.LastDamageInfo;
                Vector2 knockback = info.knockbackDir * info.knockbackForce;
                knockback.y = owner.HurtLaunchUpForce;
                owner.Rb.AddForce(knockback, ForceMode2D.Impulse);
            }
            return;
        }

        // 阶段二：击退硬直，用正常 deltaTime 计时
        timer += Time.deltaTime;
        if (timer >= owner.KnockbackDuration)
        {
            if (owner.isGrounded)
                hfsm.SwitchState(E_PlayerStateType.Grounded);
            else
                hfsm.SwitchState(E_PlayerStateType.AirBornd);
        }
    }

    public override void OnExit()
    {
        // 安全兜底
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
