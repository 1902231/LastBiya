using UnityEngine;

/// <summary>
/// 下冲二段：落地后向面朝方向水平滑行
/// 无敌状态，接受攻击等输入
/// 碰到墙壁、平台边缘或持续时间结束后进入 Grounded
/// </summary>
public class PlayerState_FallingDashSlide : HFSM_BaseState<E_PlayerStateType, PlayerController>
{
    private float timer;

    public PlayerState_FallingDashSlide()
    {
        this.parentType = E_PlayerStateType.Alive;
    }

    public override void OnEnter()
    {
        timer = 0f;

        // 开启无敌
        owner.AbilityMgr.TryActivate(E_PlayerAbilityType.Invincible);

        // 给一个初始水平速度
        owner.Rb.velocity = new Vector2(owner.FacingDirection * owner.FallingDashSlideForce, 0f);
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;

        // 攻击输入
        if (InputManager.Instance.Consume(InputManager.Instance.AttackAction))
        {
            owner.AbilityMgr.TryActivate(E_PlayerAbilityType.Attack);
        }

        // 时间到
        if (timer >= owner.FallingDashSlideDuration)
        {
            hfsm.SwitchState(E_PlayerStateType.Grounded);
            return;
        }

        // 碰墙
        if (owner.IsWallAhead())
        {
            hfsm.SwitchState(E_PlayerStateType.Grounded);
            return;
        }

        // 离地（平台边缘）
        if (!owner.isGrounded)
        {
            hfsm.SwitchState(E_PlayerStateType.Grounded);
            return;
        }
    }

    public override void OnFixedUpdate()
    {
        // 持续施加水平力维持滑行
        owner.Rb.AddForce(Vector2.right * owner.FacingDirection * owner.FallingDashSlideForce, ForceMode2D.Force);
    }

    public override void OnExit()
    {
        // 关闭无敌
        var invincible = owner.AbilityMgr.Get(E_PlayerAbilityType.Invincible);
        invincible?.Deactivate();

        owner.Rb.velocity = new Vector2(0, owner.Rb.velocity.y);
    }
}
