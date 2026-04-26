using UnityEngine;

/// <summary>
/// 死亡状态
/// </summary>
public class Boss_DeadState : HFSM_BaseState<E_BossStateType_01, Boss01>
{
    public override void OnEnter()
    {
        // 停止移动
        owner.Rb.velocity = Vector2.zero;
        
        // 禁用碰撞
        var collider = owner.GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;

        // TODO: 播放死亡动画、特效等
        Debug.Log("Boss 已死亡！");
    }

    public override void OnUpdate()
    {
        // 死亡状态不做任何事
    }
}
