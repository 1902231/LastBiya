using UnityEngine;

public class PlayerState_DoubleJump : HFSM_BaseState<E_PlayerStateType, PlayerController>
{
    public PlayerState_DoubleJump()
    {
        this.parentType = E_PlayerStateType.AirBornd;
    }

    private bool hasReleasedJump;
    private bool shouldCutJump;

    public override void OnEnter()
    {
        hasReleasedJump = false;
        shouldCutJump = false;
        // 二段跳赋速度（复用 JumpForce，以后可以加独立参数）
        owner.Rb.velocity = new Vector2(owner.Rb.velocity.x, owner.JumpForce);
        // 以后在这里播放二段跳动画、特效等
    }

    public override void OnUpdate()
    {
        // 下落冲刺
        if (InputManager.Instance.Consume(InputManager.Instance.FallingDashAction)
            && owner.AbilityMgr.TryActivate(E_PlayerAbilityType.FallingDash))
        {
            hfsm.SwitchState(E_PlayerStateType.FallingDash);
            return;
        }

        // 冲刺
        if (InputManager.Instance.DashAction.WasPressedThisFrame()
            && owner.AbilityMgr.TryActivate(E_PlayerAbilityType.Dash))
        {
            hfsm.SwitchState(E_PlayerStateType.Dash);
            return;
        }

        // 松手截断标记
        if (!hasReleasedJump && InputManager.Instance.JumpAction.WasReleasedThisFrame())
        {
            hasReleasedJump = true;
            shouldCutJump = true;
        }
    }

    public override void OnFixedUpdate()
    {
        if (shouldCutJump)
        {
            shouldCutJump = false;
            if (owner.Rb.velocity.y > 0)
            {
                owner.Rb.velocity = new Vector2(owner.Rb.velocity.x, owner.Rb.velocity.y * owner.JumpCutMultiplier);
            }
        }

        if (owner.Rb.velocity.y <= 0)
        {
            hfsm.SwitchState(E_PlayerStateType.FreeFall);
        }

        owner.ApplyHorizontalMovement(InputManager.Instance.MoveInput.x);
    }

    public override void OnExit()
    {
    }
}
