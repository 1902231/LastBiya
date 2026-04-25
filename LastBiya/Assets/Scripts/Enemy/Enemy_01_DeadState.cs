using UnityEngine;

/// <summary>
/// 死亡状态：禁用碰撞，以后加死亡动画和销毁
/// </summary>
public class Enemy_01_DeadState : HFSM_BaseState<E_Enemy01StateType, Enemy_01>
{
    // 无 parent，和 Alive 平级

    public override void OnEnter()
    {
        owner.Rb.velocity = Vector2.zero;

        // 禁用碰撞体，防止继续被攻击或挡路
        var col = owner.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 以后在这里播放死亡动画、延迟销毁等
        // GameObject.Destroy(owner.gameObject, 1f);
    }
}
