using UnityEngine;

public class Enemy_02_DeadState : HFSM_BaseState<E_Enemy02StateType, Enemy_02>
{
    public override void OnEnter()
    {
        owner.Rb.velocity = Vector2.zero;
        var col = owner.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }
}
