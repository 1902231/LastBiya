using UnityEngine;

public class Enemy_02_DeadState : HFSM_BaseState<E_Enemy02StateType, Enemy_02>
{
    private bool hasLanded;

    public override void OnEnter()
    {
        owner.Rb.velocity = new Vector2(0, owner.Rb.velocity.y);
        hasLanded = false;

        SetLayerRecursively(owner.gameObject, LayerMask.NameToLayer("DeadUnit"));
    }

    public override void OnFixedUpdate()
    {
        if (hasLanded) return;

        owner.Rb.velocity = new Vector2(0, owner.Rb.velocity.y);

        if (IsGrounded())
        {
            hasLanded = true;
            owner.Rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private bool IsGrounded()
    {
        Vector2 checkPos = (Vector2)owner.transform.position + Vector2.down * 0.5f;
        return Physics2D.OverlapCircle(checkPos, 0.1f, owner.groundLayer);
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
