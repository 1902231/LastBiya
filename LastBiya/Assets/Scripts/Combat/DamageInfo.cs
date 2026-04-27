using UnityEngine;

public enum DamageSource
{

    Enemy,
    Player,
    Tra,
}

/// <summary>
/// 伤害信息结构体，在攻击方和受击方之间传递
/// </summary>
public struct DamageInfo
{
    public int damage;
    public float postureDamage;
    public Vector2 knockbackDir;
    public float knockbackForce;
    public DamageSource source;

    public DamageInfo(int damage, Vector2 knockbackDir, float knockbackForce, DamageSource source = DamageSource.Enemy, float postureDamage = 0f)
    {
        this.damage = damage;
        this.postureDamage = postureDamage;
        this.knockbackDir = knockbackDir;
        this.knockbackForce = knockbackForce;
        this.source = source;
    }
}
