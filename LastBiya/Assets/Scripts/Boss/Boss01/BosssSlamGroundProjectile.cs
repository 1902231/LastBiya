using UnityEngine;

/// <summary>
/// 下砸贴地子弹
/// 根物体：Rigidbody2D + 普通 Collider（非 Trigger），靠物理碰撞贴地
/// 子物体：AttackHitbox + Trigger Collider，负责伤害判定
/// 水平射线检测墙壁，碰墙自毁
/// </summary>
public class BosssSlamGroundProjectile : MonoBehaviour
{
    private int direction;
    private float speed;
    private float wallCheckDistance;
    private LayerMask wallLayer;
    private Rigidbody2D rb;

    public void Initialize(int dir, float spd, float dmg, float postureDmg, float wallDist, LayerMask wallMask)
    {
        direction = dir;
        speed = spd;
        wallCheckDistance = wallDist;
        wallLayer = wallMask;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = new Vector2(direction * speed, rb.velocity.y);

        // 从子物体获取 AttackHitbox 设置伤害
        var hitbox = GetComponentInChildren<AttackHitbox>(true);
        if (hitbox != null)
        {
            hitbox.damage = (int)dmg;
            hitbox.postureDamage = postureDmg;
        }
    }

    void FixedUpdate()
    {
        // 保持水平速度，垂直速度交给重力
        if (rb != null)
            rb.velocity = new Vector2(direction * speed, rb.velocity.y);
    }

    void Update()
    {
        // 射线检测墙壁（从中心稍微抬高，避免擦到地面）
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.3f;
        Vector2 rayDir = Vector2.right * direction;
        
        if (Physics2D.Raycast(origin, rayDir, wallCheckDistance, wallLayer))
        {
            Destroy(gameObject);
        }

        // 可视化射线
        Debug.DrawRay(origin, rayDir * wallCheckDistance, Color.red);
    }
}
