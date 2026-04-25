using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState_FreeFall :  HFSM_BaseState<E_PlayerStateType, PlayerController>
{
    public PlayerState_FreeFall()
    {
        this.parentType = E_PlayerStateType.AirBornd;
    }

    private float enterTimer;
    private static float groundCheckDelay = 0f;

    /// <summary>
    /// 外部调用：下次进入 FreeFall 时延迟落地检测（用于冲刺弹起等场景）
    /// </summary>
    public static void RequestGroundCheckDelay(float delay)
    {
        groundCheckDelay = delay;
    }

    public override void OnEnter()
    {
        enterTimer = 0f;
    }

    public override void OnUpdate()
    {
        enterTimer += Time.deltaTime;

        // 下落冲刺（优先于普通冲刺）
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

        // 二段跳，实时检测
        if (InputManager.Instance.JumpAction.WasPressedThisFrame())
        {
            if (owner.AbilityMgr.TryActivate(E_PlayerAbilityType.DoubleJump))
            {
                hfsm.SwitchState(E_PlayerStateType.DoubleJump);
                return;
            }
        }

        // 落地检测（有延迟请求时等延迟结束再检测）
        if (enterTimer > groundCheckDelay && owner.isGrounded)
        {
            hfsm.SwitchState(E_PlayerStateType.Grounded);
            return;
        }
    }

    public override void OnFixedUpdate()
    {
        Vector2 input = InputManager.Instance.MoveInput;
        owner.ApplyHorizontalMovement(input.x);
    }


    public override void OnExit()
    {
        groundCheckDelay = 0f;
    }
}
