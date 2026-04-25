using UnityEngine;

/// <summary>
/// 蓄力攻击能力（持续型）
/// 按住蓄力 → 蓄满后启用 ChargeHitbox → 松开或释放完毕自动结束
/// priority = 2，蓄力期间除受伤外不可被打断
/// </summary>
public class PlayerAbility_ChargeAttack : BaseAbility<PlayerController>
{
    private float chargeTimer;
    private float releaseTimer;
    private bool isReleased;
    private AttackHitbox chargeHitbox;

    public PlayerAbility_ChargeAttack()
    {
        priority = 2;
    }

    public override bool CanActivate()
    {
        return isUnlocked && !isActive;
    }

    public override bool CanBeInterrupted()
    {
        // 蓄力期间不可被打断（只有受伤 priority=3 能通过优先级压制）
        return false;
    }

    public override void Activate()
    {
        base.Activate();
        chargeTimer = 0f;
        releaseTimer = 0f;
        isReleased = false;

        // 蓄力期间冻结水平速度
        owner.Rb.velocity = new Vector2(0, owner.Rb.velocity.y);

        // 查找 ChargeHitbox
        if (chargeHitbox == null)
            FindChargeHitbox();
    }

    public override void Tick(float deltaTime)
    {
        // 阶段一：蓄力中
        if (!isReleased)
        {
            chargeTimer += deltaTime;

            // 持续冻结水平速度
            owner.Rb.velocity = new Vector2(0, owner.Rb.velocity.y);

            // 松开按键 → 蓄力取消
            if (InputManager.Instance.ChargeAttackAction.WasReleasedThisFrame())
            {
                Deactivate();
                return;
            }

            // 蓄力完成 → 释放攻击
            if (chargeTimer >= owner.ChargeTime)
            {
                isReleased = true;
                releaseTimer = 0f;

                if (chargeHitbox != null)
                {
                    chargeHitbox.damage = (int)owner.ChargeDamage;
                    chargeHitbox.ResetHitRecord();
                    chargeHitbox.gameObject.SetActive(true);
                }
            }
            return;
        }

        // 阶段二：释放中（Hitbox 激活）
        releaseTimer += deltaTime;
        if (releaseTimer >= owner.ChargeReleaseDuration)
        {
            Deactivate();
        }
    }

    public override void Deactivate()
    {
        base.Deactivate();
        if (chargeHitbox != null)
            chargeHitbox.gameObject.SetActive(false);
    }

    private void FindChargeHitbox()
    {
        var hitboxes = owner.GetComponentsInChildren<AttackHitbox>(true);
        foreach (var hb in hitboxes)
        {
            if (hb.gameObject.name.Contains("Charge"))
            {
                chargeHitbox = hb;
                return;
            }
        }
    }
}
