using UnityEngine;

/// <summary>
/// 远程攻击状态
/// 流程：前摇 → 发射子弹 → 子弹飞出(撞墙或到达最大距离停止) → 停顿 → 子弹返回Boss → 状态完成
/// Boss 全程不移动
/// </summary>
public class Boss_RangedAttackState : HFSM_BaseState<E_BossStateType_01, Boss01>
{
    private float timer;
    private Phase currentPhase;
    private BossProjectile activeProjectile;

    private enum Phase
    {
        Windup,             // 前摇
        WaitForProjectile   // 等待子弹完成整个生命周期
    }

    public Boss_RangedAttackState()
    {
        this.parentType = E_BossStateType_01.Alive;
    }

    public override void OnEnter()
    {
        timer = 0f;
        currentPhase = Phase.Windup;
        activeProjectile = null;

        owner.StopHorizontalMovement();
        owner.FacePlayer();
    }

    public override void OnUpdate()
    {
        switch (currentPhase)
        {
            case Phase.Windup:
                timer += Time.deltaTime;
                if (timer >= owner.rangedWindupDuration)
                {
                    SpawnProjectile();
                    currentPhase = Phase.WaitForProjectile;
                }
                break;

            case Phase.WaitForProjectile:
                // 子弹销毁后引用变为 null，视为完成
                break;
        }
    }

    public override void OnFixedUpdate()
    {
        owner.StopHorizontalMovement();
    }

    public override void OnExit()
    {
        // 如果状态被强制打断，清理残留子弹
        if (activeProjectile != null)
            Object.Destroy(activeProjectile.gameObject);
    }

    private void SpawnProjectile()
    {
        if (owner.rangedProjectilePrefab == null)
        {
            Debug.LogWarning("Boss 远程攻击预制体未设置！");
            return;
        }

        Vector3 spawnPos = owner.transform.position + Vector3.right * owner.FacingDirection * 1f;
        GameObject obj = Object.Instantiate(owner.rangedProjectilePrefab, spawnPos, Quaternion.identity);

        activeProjectile = obj.GetComponent<BossProjectile>();
        if (activeProjectile != null)
        {
            activeProjectile.Initialize(
                owner.FacingDirection,
                owner.projectileSpeed,
                owner.projectileMaxDistance,
                owner.projectileDamage,
                owner.transform,
                owner.rangedRecoveryDuration  // 复用后摇时间作为停顿时长
            );
        }
    }

    /// <summary>
    /// 攻击完成：子弹已返回Boss并销毁
    /// </summary>
    public bool IsAttackComplete()
    {
        return currentPhase == Phase.WaitForProjectile && activeProjectile == null;
    }
}
