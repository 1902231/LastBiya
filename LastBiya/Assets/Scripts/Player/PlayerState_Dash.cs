using UnityEngine;

public class PlayerState_Dash : HFSM_BaseState<E_PlayerStateType, PlayerController>
{

    private float timer;
    private Vector2 dashDir;
    private float originalGravity;
    private AttackHitbox dashHitbox;
    private bool hasBounced; // 防止多次弹起
    public PlayerState_Dash()
    {
        this.parentType = E_PlayerStateType.Alive;
    }

    public override void OnEnter()
    {
        timer = owner.DashDuration;
        hasBounced = false;

        // 冲刺方向
        Vector2 input = InputManager.Instance.MoveInput;
        if (input.x != 0)
            dashDir = new Vector2(Mathf.Sign(input.x), 0);
        else
            dashDir = new Vector2(owner.FacingDirection, 0);

        // 关闭重力
        originalGravity = owner.Rb.gravityScale;
        owner.Rb.gravityScale = 0f;
        owner.Rb.velocity = dashDir * owner.DashSpeed;

        // 启用 DashHitbox（名字包含 "Dash" 的子物体）
        if (dashHitbox == null)
            dashHitbox = FindDashHitbox();

        if (dashHitbox != null)
        {
            dashHitbox.ResetHitRecord();
            dashHitbox.onHitCallback = OnDashHit;
            dashHitbox.gameObject.SetActive(true);
        }
    }

    public override void OnUpdate()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            if (owner.isGrounded)
                hfsm.SwitchState(E_PlayerStateType.Grounded);
            else
                hfsm.SwitchState(E_PlayerStateType.AirBornd);
        }
    }

    public override void OnFixedUpdate()
    {
        if (!hasBounced)
            owner.Rb.velocity = dashDir * owner.DashSpeed;
    }

    public override void OnExit()
    {
        owner.Rb.gravityScale = originalGravity;

        if (!hasBounced)
            owner.Rb.velocity = Vector2.zero;

        // 禁用 DashHitbox，清除回调
        if (dashHitbox != null)
        {
            dashHitbox.gameObject.SetActive(false);
            dashHitbox.onHitCallback = null;
        }
    }

    /// <summary>
    /// 冲刺命中敌人时的回调：反方向弹起
    /// </summary>
    private void OnDashHit(Collider2D other)
    {
        if (hasBounced) return;
        hasBounced = true;

        // 恢复重力，反方向弹起
        owner.Rb.gravityScale = originalGravity;
        // 直接设速度而不是 AddForce，确保同帧生效
        Vector2 bounceDir = new Vector2(-dashDir.x, 2f).normalized;
        owner.Rb.velocity = bounceDir * owner.DashBounceForce;

        // 请求 FreeFall 延迟落地检测，防止弹起被立刻拉回地面
        PlayerState_FreeFall.RequestGroundCheckDelay(0.1f);

        // 立刻切到空中状态
        hfsm.SwitchState(E_PlayerStateType.AirBornd);
    }

    private AttackHitbox FindDashHitbox()
    {
        var hitboxes = owner.GetComponentsInChildren<AttackHitbox>(true);
        foreach (var hb in hitboxes)
        {
            if (hb.gameObject.name.Contains("Dash"))
                return hb;
        }
        return null;
    }
}
