using UnityEngine;

public class PlayerState_FallingDash : HFSM_BaseState<E_PlayerStateType, PlayerController>
{
    private float originalGravity;
    private Vector2 dashDir;
    private AttackHitbox fallingDashHitbox;
    private float timer;
    private Phase currentPhase;

    private enum Phase
    {
        Windup,     // 前摇：可被打断，不能动
        Active      // 下冲中
    }

    public PlayerState_FallingDash()
    {
        this.parentType = E_PlayerStateType.AirBornd;
    }

    public override void OnEnter()
    {
        timer = 0f;
        currentPhase = Phase.Windup;
        originalGravity = owner.Rb.gravityScale;

        // 前摇阶段：冻结速度，悬停在空中
        owner.Rb.gravityScale = 0f;
        owner.Rb.velocity = Vector2.zero;

        // 预先查找 hitbox（不启用）
        if (fallingDashHitbox == null)
            FindFallingDashHitbox();
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;

        switch (currentPhase)
        {
            case Phase.Windup:
                if (timer >= owner.FallingDashWindupDuration)
                {
                    StartDash();
                }
                break;

            case Phase.Active:
                // 墙壁检测优先于落地检测
                if (owner.IsWallOnEitherSide())
                {
                    hfsm.SwitchState(E_PlayerStateType.FreeFall);
                    return;
                }

                // 落地 → 进入下冲二段滑行
                if (owner.isGrounded)
                {
                    hfsm.SwitchState(E_PlayerStateType.FallingDashSlide);
                }
                break;
        }
    }

    public override void OnFixedUpdate()
    {
        // 前摇阶段保持静止
        if (currentPhase == Phase.Windup)
        {
            owner.Rb.velocity = Vector2.zero;
        }
    }

    public override void OnExit()
    {
        owner.Rb.gravityScale = originalGravity;
        owner.Rb.velocity = Vector2.zero;
        owner.IsInvincible = false;

        if (fallingDashHitbox != null)
            fallingDashHitbox.gameObject.SetActive(false);
    }

    private void StartDash()
    {
        currentPhase = Phase.Active;

        // 计算冲刺方向
        float rad = owner.FallingDashAngle * Mathf.Deg2Rad;
        dashDir = new Vector2(Mathf.Sin(rad) * owner.FacingDirection, -Mathf.Cos(rad)).normalized;

        // 施加速度
        owner.Rb.velocity = dashDir * owner.FallingDashSpeed;

        // 下冲阶段无敌
        owner.IsInvincible = true;

        // 启用 hitbox
        if (fallingDashHitbox != null)
        {
            fallingDashHitbox.damage = (int)owner.FallingDashDamage;
            fallingDashHitbox.ResetHitRecord();
            fallingDashHitbox.gameObject.SetActive(true);
        }
    }

    private void FindFallingDashHitbox()
    {
        var hitboxes = owner.GetComponentsInChildren<AttackHitbox>(true);
        foreach (var hb in hitboxes)
        {
            if (hb.gameObject.name.Contains("FallingDash"))
            {
                fallingDashHitbox = hb;
                return;
            }
        }
    }
}
