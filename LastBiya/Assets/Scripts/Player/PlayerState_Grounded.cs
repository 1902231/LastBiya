using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState_Grounded : HFSM_BaseState<E_PlayerStateType,PlayerController>
{
    private float coyoteTimer;

    public PlayerState_Grounded()
    {
        this.parentType = E_PlayerStateType.Alive;
        this.defaultChildType = E_PlayerStateType.Idle;
    }

    public override void OnEnter()
    {
        coyoteTimer = 0f;
        // 落地重置二段跳
        var dj = owner.AbilityMgr.Get<PlayerAbility_DoubleJump>(E_PlayerAbilityType.DoubleJump);
        dj?.ResetJumps();
    }

    public override void OnUpdate()
    {
        // 冲刺优先级最高（仅次于受伤），无缓冲，实时检测
        if (InputManager.Instance.DashAction.WasPressedThisFrame()
            && owner.AbilityMgr.TryActivate(E_PlayerAbilityType.Dash))
        {
            hfsm.SwitchState(E_PlayerStateType.Dash);
            return;
        }

        // 蓄力攻击（仅地面可用）
        if (InputManager.Instance.ChargeAttackAction.WasPressedThisFrame()
            && owner.AbilityMgr.TryActivate(E_PlayerAbilityType.ChargeAttack))
        {
            hfsm.SwitchState(E_PlayerStateType.ChargeAttack);
            return;
        }

        // 跳跃优先判断（在土狼时间内仍然允许）
        if (InputManager.Instance.Consume(InputManager.Instance.JumpAction))
        {
            hfsm.SwitchState(E_PlayerStateType.Jump);
            return;
        }

        // 离地后开始土狼时间倒计时，到期才真正切走
        if (!owner.isGrounded)
        {
            coyoteTimer += Time.deltaTime;
            if (coyoteTimer >= owner.CoyoteTime)
            {
                hfsm.SwitchState(E_PlayerStateType.AirBornd);
            }
            return;
        }
        else
        {
            coyoteTimer = 0f;
        }

        Vector2 input = InputManager.Instance.MoveInput;
        if (input.x == 0)
        { 
            hfsm.SwitchState(E_PlayerStateType.Idle);
        }

        
    }


    public override void OnExit()
    {
        //Debug.Log("�˳�״̬��Grounded");
        //owner.isGrounded = false;

    }
}
