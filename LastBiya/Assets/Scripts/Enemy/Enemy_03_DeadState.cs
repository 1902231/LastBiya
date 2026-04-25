using UnityEngine;

public class Enemy_03_DeadState : HFSM_BaseState<E_Enemy03StateType, Enemy_03>
{
    public override void OnEnter()
    {
        owner.Rb.velocity = Vector2.zero;
        owner.Rb.gravityScale = 1f; // 死后恢复重力，尸体掉落
        var col = owner.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }
}
