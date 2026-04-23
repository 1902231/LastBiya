using UnityEngine;

/// <summary>
/// 蓄力攻击状态
/// 按住蓄力 → 蓄满后启用 ChargeHitbox → 松开或释放完毕回 Idle
/// 蓄力期间只有受伤能打断（受伤通过事件中心切 Heart，不经过这里的判断）
/// </summary>
public class PlayerState_ChargeAttack : HFSM_BaseState<E_PlayerStateType, PlayerController>
{
    private float chargeTimer;
    private float releaseTimer;
    private bool isReleased; // 蓄力完成，进入释放阶段
    private AttackHitbox chargeHitbox;

    public PlayerState_ChargeAttack()
    {
        this.parentType = E_PlayerStateType.Alive;
    }

    public override void OnEnter()
    {
        chargeTimer = 0f;
        releaseTimer = 0f;
        isReleased = false;

        // 蓄力期间停止移动
        owner.Rb.velocity = Vector2.zero;

        // 查找 ChargeHitbox（带 "ChargeHitbox" tag 或第二个 Hitbox）
        if (chargeHitbox == null)
            chargeHitbox = FindChargeHitbox();
    }

    public override void OnUpdate()
    {
        // 阶段一：蓄力中
        if (!isReleased)
        {
            chargeTimer += Time.deltaTime;

            // 松开按键 → 蓄力取消，回 Idle
            if (InputManager.Instance.ChargeAttackAction.WasReleasedThisFrame())
            {
                hfsm.SwitchState(E_PlayerStateType.Grounded);
                return;
            }

            // 蓄力完成 → 释放攻击
            if (chargeTimer >= owner.ChargeTime)
            {
                isReleased = true;
                releaseTimer = 0f;

                if (chargeHitbox != null)
                {
                    chargeHitbox.ResetHitRecord();
                    chargeHitbox.gameObject.SetActive(true);
                }
            }
            return;
        }

        // 阶段二：释放中（Hitbox 激活）
        releaseTimer += Time.deltaTime;
        if (releaseTimer >= owner.ChargeReleaseDuration)
        {
            hfsm.SwitchState(E_PlayerStateType.Grounded);
        }
    }

    public override void OnExit()
    {
        if (chargeHitbox != null)
            chargeHitbox.gameObject.SetActive(false);
    }

    private AttackHitbox FindChargeHitbox()
    {
        // 查找名字包含 "Charge" 的 Hitbox 子物体
        var hitboxes = owner.GetComponentsInChildren<AttackHitbox>(true);
        foreach (var hb in hitboxes)
        {
            if (hb.gameObject.name.Contains("Charge"))
                return hb;
        }
        return null;
    }
}
