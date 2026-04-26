using UnityEngine;

/// <summary>
/// 下砸状态：向下冲刺并造成伤害
/// 碰到墙壁时提前视为落地
/// </summary>
public class Boss_SlamDownState : HFSM_BaseState<E_BossStateType_01, Boss01>
{
    private bool hasLanded;
    private float recoveryTimer;

    public Boss_SlamDownState()
    {
        this.parentType = E_BossStateType_01.Alive;
    }

    public override void OnEnter()
    {
        hasLanded = false;
        recoveryTimer = 0f;
        ActivateHitbox();
        
        // 锁定X轴，防止圆形碰撞体撞地面产生水平滑动
        owner.Rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        
        // 立即给予向下的初始速度，不要有加速过程
        owner.Rb.velocity = Vector2.down * owner.slamDownSpeed;
    }

    public override void OnUpdate()
    {
        if (!hasLanded)
        {
            // 地面检测 或 射线检测到下方墙壁
            bool hitWall = owner.CheckWallInDirection(Vector2.down);
            if (owner.IsGrounded || hitWall)
            {
                hasLanded = true;
                DeactivateHitbox();
            }
        }

        if (hasLanded)
        {
            recoveryTimer += Time.deltaTime;
        }
    }

    public override void OnFixedUpdate()
    {
        if (!hasLanded)
        {
            // 下砸是恒定速度冲刺，直接设置速度
            owner.Rb.velocity = Vector2.down * owner.slamDownSpeed;
        }
    }

    public override void OnExit()
    {
        DeactivateHitbox();
        // 解除X轴锁定，恢复只锁旋转
        owner.Rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        owner.Rb.velocity = new Vector2(0, owner.Rb.velocity.y);
    }

    private void ActivateHitbox()
    {
        if (owner.slamHitbox != null)
        {
            owner.slamHitbox.gameObject.SetActive(true);
            owner.slamHitbox.damage = (int)owner.slamDownDamage;
        }
    }

    private void DeactivateHitbox()
    {
        if (owner.slamHitbox != null)
        {
            owner.slamHitbox.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 检查是否完成下砸
    /// </summary>
    public bool IsSlamComplete()
    {
        return hasLanded && recoveryTimer >= owner.slamDownRecoveryDuration;
    }
}
