using UnityEngine;

/// <summary>
/// 攻击状态：播放攻击，启用 Hitbox，攻击结束后回追逐
/// </summary>
public class Enemy_01_AttackState : HFSM_BaseState<E_Enemy01StateType, Enemy_01>
{
    private float timer;
    private AttackHitbox hitbox;

    public Enemy_01_AttackState()
    {
        this.parentType = E_Enemy01StateType.Alive;
    }

    public override void OnEnter()
    {
        timer = 0f;
        owner.Rb.velocity = new Vector2(0, owner.Rb.velocity.y); // 攻击时停下

        // 启用 Hitbox
        hitbox = owner.attackHitbox;

        if (hitbox != null)
        {
            hitbox.ResetHitRecord();
            hitbox.gameObject.SetActive(true);
        }
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;

        // 判定帧结束，关闭 Hitbox（前半段）
        if (timer >= owner.attackDuration * 0.5f && hitbox != null && hitbox.gameObject.activeSelf)
        {
            hitbox.gameObject.SetActive(false);
        }

        // 攻击结束
        if (timer >= owner.attackDuration)
        {
            hfsm.SwitchState(E_Enemy01StateType.Chase);
        }
    }

    public override void OnExit()
    {
        owner.attackCooldownTimer = owner.attackCooldown;

        if (hitbox != null)
            hitbox.gameObject.SetActive(false);
    }
}
