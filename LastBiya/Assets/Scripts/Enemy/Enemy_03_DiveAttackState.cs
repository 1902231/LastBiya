using UnityEngine;

/// <summary>
/// 俯冲攻击状态：朝玩家位置高速冲刺，到达或超时后回到跟踪状态
/// </summary>
public class Enemy_03_DiveAttackState : HFSM_BaseState<E_Enemy03StateType, Enemy_03>
{
    private float timer;
    private Vector2 diveDir;
    private AttackHitbox hitbox;

    public Enemy_03_DiveAttackState()
    {
        this.parentType = E_Enemy03StateType.Alive;
    }

    public override void OnEnter()
    {
        timer = 0f;

        // 锁定俯冲方向（进入时确定，不追踪）
        if (owner.PlayerTransform != null)
            diveDir = ((Vector2)owner.PlayerTransform.position - (Vector2)owner.transform.position).normalized;
        else
            diveDir = new Vector2(owner.FacingDirection, -1f).normalized;

        owner.Rb.velocity = diveDir * owner.diveSpeed;

        // 启用 Hitbox
        if (hitbox == null)
            hitbox = owner.GetComponentInChildren<AttackHitbox>(true);

        if (hitbox != null)
        {
            hitbox.ResetHitRecord();
            hitbox.gameObject.SetActive(true);
        }
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;

        // 到达玩家附近 或 超时 → 回到跟踪
        bool arrived = owner.PlayerTransform != null &&
            Vector2.Distance(owner.transform.position, owner.PlayerTransform.position) <= owner.diveArriveDistance;

        if (arrived || timer >= owner.diveDuration)
        {
            hfsm.SwitchState(E_Enemy03StateType.Tracking);
        }
    }

    public override void OnExit()
    {
        owner.Rb.velocity = Vector2.zero;

        if (hitbox != null)
            hitbox.gameObject.SetActive(false);
    }
}
