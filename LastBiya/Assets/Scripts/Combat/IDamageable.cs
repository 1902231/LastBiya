using UnityEngine;

/// <summary>
/// 伤害接口，所有可被攻击的对象实现此接口
/// </summary>
public interface IDamageable
{
    void TakeDamage(DamageInfo info);
}
