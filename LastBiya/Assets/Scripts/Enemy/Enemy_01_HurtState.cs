using UnityEngine;

/// <summary>
/// 受击状态：被击退，硬直结束后回追逐或巡逻
/// </summary>
public class Enemy_01_HurtState : HFSM_BaseState<E_Enemy01StateType, Enemy_01>
{
    private float timer;

    public Enemy_01_HurtState()
    {
        this.parentType = E_Enemy01StateType.Alive;
    }

    public override void OnEnter()
    {
        timer = 0f;

        // 施加击退力
        var info = owner.LastDamageInfo;
        owner.Rb.velocity = Vector2.zero;
        Vector2 knockback = info.knockbackDir * info.knockbackForce;
        knockback.y = owner.hurtLaunchUpForce;
        owner.Rb.AddForce(knockback, ForceMode2D.Impulse);
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;
        if (timer >= owner.hurtDuration)
        {
            if (owner.DetectPlayer())
                hfsm.SwitchState(E_Enemy01StateType.Chase);
            else
                hfsm.SwitchState(E_Enemy01StateType.Patrol);
        }
    }
}
