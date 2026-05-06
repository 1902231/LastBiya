using UnityEngine;

public class PlayerState_Dash : HFSM_BaseState<E_PlayerStateType, PlayerController>
{

    private float timer;
    private Vector2 dashDir;
    private float originalGravity;
    private AttackHitbox dashHitbox;
    private bool hasBounced;
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

        // 启用 DashHitbox（名字包含 "Dash" 的子物体）
        if (dashHitbox == null)
            dashHitbox = FindDashHitbox();

        if (dashHitbox != null)
        {
            dashHitbox.postureDamage = owner.DashPostureDamage;
            dashHitbox.ResetHitRecord();
            dashHitbox.onHitCallback = OnDashHit;
            dashHitbox.gameObject.SetActive(true);
        }

        // 关闭重力，施加冲刺速度
        originalGravity = owner.Rb.gravityScale;
        owner.Rb.gravityScale = 0f;
        owner.Rb.velocity = dashDir * owner.DashSpeed;
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
        // 回血 + 广播事件
        owner.Heal(owner.DashLifeSteal);
        EventCenter.Instance.EventTrigger("PlayerHitEnemy", other.transform);

        if (hasBounced) return;
        hasBounced = true;

        // 恢复重力，按角度反弹
        owner.Rb.gravityScale = originalGravity;
        float rad = owner.DashBounceAngle * Mathf.Deg2Rad;
        Vector2 bounceDir = new Vector2(-dashDir.x * Mathf.Sin(rad), Mathf.Cos(rad)).normalized;
        owner.Rb.velocity = bounceDir * owner.DashBounceForce;

        // 切到回弹状态（由回弹状态管理无敌）
        hfsm.SwitchState(E_PlayerStateType.DashBounce);
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
