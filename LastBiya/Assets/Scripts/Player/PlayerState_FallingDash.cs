using UnityEngine;

public class PlayerState_FallingDash : HFSM_BaseState<E_PlayerStateType, PlayerController>
{
    public PlayerState_FallingDash()
    {
        this.parentType = E_PlayerStateType.AirBornd;
    }

    private float originalGravity;
    private Vector2 dashDir;

    public override void OnEnter()
    {
        // 根据角度和朝向计算冲刺方向
        float rad = owner.FallingDashAngle * Mathf.Deg2Rad;
        dashDir = new Vector2(Mathf.Sin(rad) * owner.FacingDirection, -Mathf.Cos(rad)).normalized;

        originalGravity = owner.Rb.gravityScale;
        owner.Rb.gravityScale = 0f;
        owner.Rb.velocity = dashDir * owner.FallingDashSpeed;
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
        // 持续锁定下冲速度，防止被物理干扰
        owner.Rb.velocity = dashDir * owner.FallingDashSpeed;
    }

    public override void OnExit()
    {
        owner.Rb.gravityScale = originalGravity;
        owner.Rb.velocity = Vector2.zero;
    }
}
