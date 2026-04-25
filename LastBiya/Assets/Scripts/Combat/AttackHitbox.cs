using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻击碰撞体，挂在攻击方的子物体上
/// 默认禁用，由 Ability 或状态在判定帧期间启用
/// 自动以根物体作为击退方向参考点
/// </summary>
public class AttackHitbox : MonoBehaviour
{
    [Header("伤害参数")]
    public int damage = 1;
    public float knockbackForce = 5f;
    public DamageSource damageSource = DamageSource.Enemy;

    // 击退方向参考点，自动取根物体
    private Transform attackerRoot;

    // 防止同一次攻击重复命中同一目标
    private HashSet<IDamageable> alreadyHit = new();

    /// <summary>
    /// 命中回调，命中目标时触发。参数是被命中对象的 Collider2D。
    /// 默认 null，不设就不触发。由使用方在启用 Hitbox 前设置，退出时清空。
    /// </summary>
    public System.Action<Collider2D> onHitCallback;

    void OnEnable()
    {
        // 每次启用时确保 attackerRoot 有值（比 Awake 更安全，因为禁用物体不执行 Awake）
        if (attackerRoot == null)
            attackerRoot = transform.root;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var target = other.GetComponent<IDamageable>();
        if (target == null) return;
        if (alreadyHit.Contains(target)) return;

        alreadyHit.Add(target);

        Vector2 knockbackDir = (other.transform.position - attackerRoot.position).normalized;
        var info = new DamageInfo(damage, knockbackDir, knockbackForce, damageSource);
        target.TakeDamage(info);

        // 触发命中回调（如果有）
        onHitCallback?.Invoke(other);
    }

    /// <summary>
    /// 每次攻击开始时调用，清空全部命中记录
    /// </summary>
    public void ResetHitRecord()
    {
        alreadyHit.Clear();
    }

    /// <summary>
    /// 目标离开碰撞区域时移除记录，下次进入可再次受伤
    /// </summary>
    void OnTriggerExit2D(Collider2D other)
    {
        var target = other.GetComponent<IDamageable>();
        if (target != null)
            alreadyHit.Remove(target);
    }
}
