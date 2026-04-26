using UnityEngine;

/// <summary>
/// 前劈攻击状态：有前摇、判定、后摇三个阶段
/// </summary>
public class Boss_SlashAttackState : HFSM_BaseState<E_BossStateType_01, Boss01>
{
    private float timer;
    private AttackPhase currentPhase;

    private enum AttackPhase
    {
        Windup,     // 前摇
        Active,     // 判定
        Recovery    // 后摇
    }

    public Boss_SlashAttackState()
    {
        this.parentType = E_BossStateType_01.Alive;
    }

    public override void OnEnter()
    {
        timer = 0f;
        currentPhase = AttackPhase.Windup;
        
        // 停止移动
        owner.StopHorizontalMovement();
        
        // 朝向玩家
        owner.FacePlayer();
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;

        switch (currentPhase)
        {
            case AttackPhase.Windup:
                if (timer >= owner.slashWindupDuration)
                {
                    currentPhase = AttackPhase.Active;
                    timer = 0f;
                    ActivateHitbox();
                }
                break;

            case AttackPhase.Active:
                if (timer >= owner.slashActiveDuration)
                {
                    currentPhase = AttackPhase.Recovery;
                    timer = 0f;
                    DeactivateHitbox();
                }
                break;

            case AttackPhase.Recovery:
                if (timer >= owner.slashRecoveryDuration)
                {
                    // 攻击完成，由 Behavior Designer 决定下一步
                    // 这里不自动切换状态
                }
                break;
        }
    }

    public override void OnFixedUpdate()
    {
        // 攻击期间保持静止
        owner.StopHorizontalMovement();
    }

    public override void OnExit()
    {
        DeactivateHitbox();
    }

    private void ActivateHitbox()
    {
        if (owner.slashHitbox != null)
        {
            owner.slashHitbox.gameObject.SetActive(true);
            owner.slashHitbox.damage = (int)owner.slashDamage;
        }
    }

    private void DeactivateHitbox()
    {
        if (owner.slashHitbox != null)
        {
            owner.slashHitbox.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 检查攻击是否完成（供 Behavior Designer 查询）
    /// </summary>
    public bool IsAttackComplete()
    {
        return currentPhase == AttackPhase.Recovery && 
               timer >= owner.slashRecoveryDuration;
    }
}
