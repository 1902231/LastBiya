using UnityEngine;

/// <summary>
/// 攻击能力（持续型）
/// 管理攻击的完整生命周期：判定帧 → 后摇
/// 通过启用/禁用 Hitbox 实现碰撞检测
/// Hitbox 从 owner 子物体中自动查找
/// </summary>
public class PlayerAbility_Attack : BaseAbility<PlayerController>
{
    public float activeDuration = 0.15f;
    public float recoveryDuration = 0.2f;

    private AttackHitbox hitbox;
    private float elapsed;
    private bool isInRecovery;

    public PlayerAbility_Attack()
    {
        priority = 1;
    }

    /// <summary>
    /// 从 owner 的子物体中自动查找 Hitbox
    /// 在 AbilityManager.AddAbilities 之后调用一次
    /// </summary>
    public void FindHitboxes()
    {
        hitbox = owner.GetComponentInChildren<AttackHitbox>(true);
    }

    public override bool CanActivate()
    {
        return isUnlocked && !isActive;
    }

    public override bool CanBeInterrupted()
    {
        // 只有后摇阶段可被打断
        return isInRecovery;
    }

    public override void Activate()
    {
        base.Activate();
        elapsed = 0f;
        isInRecovery = false;

        if (hitbox != null)
        {
            hitbox.damage = (int)owner.damage;
            hitbox.ResetHitRecord();
            hitbox.gameObject.SetActive(true);
        }
    }

    public override void Tick(float deltaTime)
    {
        elapsed += deltaTime;

        // 判定帧结束 → 进入后摇
        if (!isInRecovery && elapsed >= activeDuration)
        {
            isInRecovery = true;
            DisableHitbox();
        }

        // 后摇结束 → 攻击完成
        if (elapsed >= activeDuration + recoveryDuration)
        {
            Deactivate();
        }
    }

    public override void Deactivate()
    {
        base.Deactivate();
        DisableHitbox();
    }

    private void DisableHitbox()
    {
        if (hitbox != null)
            hitbox.gameObject.SetActive(false);
    }
}
