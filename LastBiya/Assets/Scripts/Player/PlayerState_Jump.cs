using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public class PlayerState_Jump : HFSM_BaseState<E_PlayerStateType, PlayerController>
{
    public PlayerState_Jump()
    {
        this.parentType = E_PlayerStateType.AirBornd;

    }

    private bool hasReleasedJump;
    private bool shouldCutJump;

    public override void OnEnter()
    {
        //Debug.Log("进入状态：Jump");
        hasReleasedJump = false;
        shouldCutJump = false;
        // 设置向上的速度实现跳跃
        owner.Rb.velocity = new Vector2(owner.Rb.velocity.x, owner.JumpForce);
    }

    public override void OnUpdate()
    {
        // 下落冲刺（优先于普通冲刺，因为是更具体的组合键）
        if (InputManager.Instance.Consume(InputManager.Instance.FallingDashAction)
            && owner.AbilityMgr.TryActivate(E_PlayerAbilityType.FallingDash))
        {
            hfsm.SwitchState(E_PlayerStateType.FallingDash);
            return;
        }

        // 冲刺，无缓冲，实时检测
        if (InputManager.Instance.DashAction.WasPressedThisFrame()
            && owner.AbilityMgr.TryActivate(E_PlayerAbilityType.Dash))
        {
            hfsm.SwitchState(E_PlayerStateType.Dash);
            return;
        }

        // 松手标记：只标记一次，实际截断在 FixedUpdate 执行
        if (!hasReleasedJump && InputManager.Instance.JumpAction.WasReleasedThisFrame())
        {
            hasReleasedJump = true;
            shouldCutJump = true;
        }

        // 二段跳：必须松开过一次才能触发
        if (hasReleasedJump && InputManager.Instance.JumpAction.WasPressedThisFrame())
        {
            if (owner.AbilityMgr.TryActivate(E_PlayerAbilityType.DoubleJump))
            {
                hfsm.SwitchState(E_PlayerStateType.DoubleJump);
                return;
            }
        }
    }

    public override void OnFixedUpdate()
    {
        // 松手截断
        if (shouldCutJump)
        {
            shouldCutJump = false;
            if (owner.Rb.velocity.y > 0)
            {
                owner.Rb.velocity = new Vector2(owner.Rb.velocity.x, owner.Rb.velocity.y * owner.JumpCutMultiplier);
            }
        }

        // 速度向下 → 转入 FreeFall
        if (owner.Rb.velocity.y <= 0)
        {
            hfsm.SwitchState(E_PlayerStateType.FreeFall);
        }

        Vector2 input = InputManager.Instance.MoveInput;
        owner.ApplyHorizontalMovement(input.x);
    }


    public override void OnExit()
    {
        //Debug.Log("退出状态：Jump");
        
    }
}
