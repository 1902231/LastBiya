using UnityEngine;

/// <summary>
/// Fighting 状态：跟随玩家锚点 + 消费目标队列发射符文
/// 队列为空且超时后切回 Normal
/// </summary>
public class BookState_Fighting : HFSM_BaseState<E_BookStateType, BookCotroller>
{
    private float idleTimer;
    private float fireTimer;

    public override void OnEnter()
    {
        idleTimer = 0f;
        fireTimer = 0f;
    }

    public override void OnUpdate()
    {
        // 有目标：重置超时计时，按间隔发射符文
        if (owner.TargetQueue.Count > 0)
        {
            idleTimer = 0f;
            fireTimer += Time.deltaTime;

            if (fireTimer >= owner.runeFireInterval)
            {
                fireTimer = 0f;
                Transform target = owner.TargetQueue.Dequeue();
                if (target != null)
                {
                    // 将目标传给 Ability，然后激活
                    var ability = owner.AbilityMgr.Get<BookAbility_RuneAttack>(E_BookAbilityType.RuneAttack);
                    if (ability != null)
                    {
                        ability.SetTarget(target);
                        owner.AbilityMgr.TryActivate(E_BookAbilityType.RuneAttack);
                    }
                }
            }
        }
        else
        {
            // 队列为空，开始超时计时
            idleTimer += Time.deltaTime;
            if (idleTimer >= owner.fightingTimeout)
            {
                hfsm.SwitchState(E_BookStateType.Normal);
            }
        }
    }
}
