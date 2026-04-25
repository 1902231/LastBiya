using UnityEngine;

public class Enemy_03_HurtState : HFSM_BaseState<E_Enemy03StateType, Enemy_03>
{
    private float timer;

    public Enemy_03_HurtState()
    {
        this.parentType = E_Enemy03StateType.Alive;
    }

    public override void OnEnter()
    {
        timer = 0f;
        var info = owner.LastDamageInfo;
        owner.Rb.velocity = Vector2.zero;
        owner.Rb.AddForce(info.knockbackDir * owner.hurtKnockbackForce, ForceMode2D.Impulse);
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;
        if (timer >= owner.hurtDuration)
        {
            // 受击后恢复，使用追击检测范围判断
            if (owner.DetectPlayer(owner.trackingDetectRange))
                hfsm.SwitchState(E_Enemy03StateType.Tracking);
            else
                hfsm.SwitchState(E_Enemy03StateType.Patrol);
        }
    }
}
