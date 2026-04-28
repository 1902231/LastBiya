using UnityEngine;

/// <summary>
/// 冲刺反弹状态
/// 进入时开启无敌，退出时关闭无敌
/// 玩家可以通过输入施加水平力，但不会像 FreeFall 那样每帧强制设置水平速度
/// 一定时间后自动进入 AirBornd
/// </summary>
public class PlayerState_DashBounce : HFSM_BaseState<E_PlayerStateType, PlayerController>
{
    private float timer;

    public PlayerState_DashBounce()
    {
        this.parentType = E_PlayerStateType.Alive;
    }

    public override void OnEnter()
    {
        timer = 0f;
        owner.AbilityMgr.TryActivate(E_PlayerAbilityType.Invincible);
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;

        if (timer >= owner.DashBounceDuration)
        {
            if (owner.isGrounded)
                hfsm.SwitchState(E_PlayerStateType.Grounded);
            else
                hfsm.SwitchState(E_PlayerStateType.AirBornd);
        }
    }

    public override void OnFixedUpdate()
    {
        // 玩家可以通过输入施加水平力，但不强制设置速度
        float inputX = InputManager.Instance.MoveInput.x;
        if (inputX != 0)
        {
            owner.Rb.AddForce(Vector2.right * inputX * owner.DashBounceAirControl, ForceMode2D.Force);
        }
    }

    public override void OnExit()
    {
        var invincible = owner.AbilityMgr.Get(E_PlayerAbilityType.Invincible);
        invincible?.Deactivate();
    }
}
