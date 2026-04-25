using UnityEngine;

public class Enemy_02_HurtState : HFSM_BaseState<E_Enemy02StateType, Enemy_02>
{
    private float timer;

    public Enemy_02_HurtState()
    {
        this.parentType = E_Enemy02StateType.Alive;
    }

    public override void OnEnter()
    {
        timer = 0f;
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
            hfsm.SwitchState(E_Enemy02StateType.Patrol);
    }
}
