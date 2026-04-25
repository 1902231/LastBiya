using UnityEngine;

public class PlayerState_FallingDash : HFSM_BaseState<E_PlayerStateType, PlayerController>
{
    public PlayerState_FallingDash()
    {
        this.parentType = E_PlayerStateType.AirBornd;
    }

    private float originalGravity;
    private Vector2 dashDir;
    private AttackHitbox fallingDashHitbox;

    public override void OnEnter()
    {
        // 根据角度和朝向计算冲刺方向
        float rad = owner.FallingDashAngle * Mathf.Deg2Rad;
        dashDir = new Vector2(Mathf.Sin(rad) * owner.FacingDirection, -Mathf.Cos(rad)).normalized;

        originalGravity = owner.Rb.gravityScale;
        owner.Rb.gravityScale = 0f;
        owner.Rb.velocity = dashDir * owner.FallingDashSpeed;

        // 下冲期间无敌（免疫敌人攻击）
        owner.IsInvincible = true;

        // 启用 FallingDash Hitbox
        if (fallingDashHitbox == null)
            FindFallingDashHitbox();

        if (fallingDashHitbox != null)
        {
            fallingDashHitbox.damage = (int)owner.FallingDashDamage;
            fallingDashHitbox.ResetHitRecord();
            fallingDashHitbox.gameObject.SetActive(true);
        }
    }

    public override void OnUpdate()
    {
        // 落地 → 回到 Grounded
        if (owner.isGrounded)
        {
            hfsm.SwitchState(E_PlayerStateType.Grounded);
        }
    }

    public override void OnFixedUpdate()
    {
        // 不再每帧强制锁速，让物理引擎自然处理碰撞
        // 重力已关闭，OnEnter 中设置的速度会保持直到碰撞
    }

    public override void OnExit()
    {
        owner.Rb.gravityScale = originalGravity;
        owner.Rb.velocity = Vector2.zero;
        owner.IsInvincible = false;

        if (fallingDashHitbox != null)
            fallingDashHitbox.gameObject.SetActive(false);
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
