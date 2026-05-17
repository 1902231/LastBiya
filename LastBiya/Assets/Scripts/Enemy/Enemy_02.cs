using UnityEngine;

/// <summary>
/// Enemy_02：行走刺球
/// 沿平台左右移动，碰到边缘掉头，身上 Hitbox 持续激活
/// 可被玩家攻击击退和击杀
/// </summary>
public class Enemy_02 : MonoBehaviour, IDamageable
{
    [Header("生命值")]
    public float maxHP = 10;
    public float currentHP;

    [Header("移动")]
    public float moveSpeed = 2f;

    [Header("受击")]
    public float hurtDuration = 0.2f;
    public float hurtLaunchUpForce = 2f;

    [Header("平台边缘检测")]
    public Vector2 edgeCheckOffset = new Vector2(0.5f, 0f);
    public float edgeCheckDistance = 1.5f;
    public float edgeCheckAngle = 45f;
    public LayerMask groundLayer;

    [Header("墙壁检测（水平射线）")]
    [Tooltip("射线发射点相对敌人中心的偏移（y 控制高度，建议略高于脚底）")]
    public Vector2 wallCheckOffset = new Vector2(0f, -0.2f);
    public float wallCheckDistance = 0.5f;

    public Rigidbody2D Rb { get; private set; }
    public int FacingDirection { get; private set; } = 1;
    public DamageInfo LastDamageInfo { get; private set; }

    public HFSM<E_Enemy02StateType, Enemy_02> StateMachine { get; private set; }

    void Start()
    {
        Rb = GetComponent<Rigidbody2D>();
        currentHP = maxHP;

        StateMachine = new HFSM<E_Enemy02StateType, Enemy_02>(this);

        StateMachine.AddState(E_Enemy02StateType.Alive,   new Enemy_02_AliveState());
        StateMachine.AddState(E_Enemy02StateType.Patrol,  new Enemy_02_PatrolState());
        StateMachine.AddState(E_Enemy02StateType.Hurt,    new Enemy_02_HurtState());
        StateMachine.AddState(E_Enemy02StateType.Dead,    new Enemy_02_DeadState());

        StateMachine.SwitchState(E_Enemy02StateType.Patrol);
    }

    void Update()
    {
        StateMachine.OnUpdate();
    }

    void FixedUpdate()
    {
        StateMachine.OnFixedUpdate();
    }

    public void UpdateFacing(int dir)
    {
        if (dir == 0) return;
        FacingDirection = dir > 0 ? 1 : -1;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * FacingDirection;
        transform.localScale = scale;
    }

    public bool HasGroundAhead(int direction)
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(edgeCheckOffset.x * direction, edgeCheckOffset.y);
        float rad = edgeCheckAngle * Mathf.Deg2Rad;
        Vector2 rayDir = new Vector2(Mathf.Sin(rad) * direction, -Mathf.Cos(rad));
        return Physics2D.Raycast(origin, rayDir, edgeCheckDistance, groundLayer);
    }

    public bool HasWallAhead(int direction)
    {
        // 射线起点需要根据方向调整 X 偏移
        Vector2 origin = (Vector2)transform.position + new Vector2(wallCheckOffset.x * direction, wallCheckOffset.y);
        Vector2 rayDir = new Vector2(direction, 0);
        return Physics2D.Raycast(origin, rayDir, wallCheckDistance, groundLayer);
    }

    public void TakeDamage(DamageInfo info)
    {
        currentHP -= info.damage;
        LastDamageInfo = info;

        if (currentHP <= 0)
            StateMachine.SwitchState(E_Enemy02StateType.Dead);
        else
            StateMachine.SwitchState(E_Enemy02StateType.Hurt);
    }

    void OnDrawGizmosSelected()
    {
        // 绘制平台边缘检测射线（斜向下）
        Gizmos.color = Color.cyan;
        for (int dir = -1; dir <= 1; dir += 2)
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(edgeCheckOffset.x * dir, edgeCheckOffset.y);
            float rad = edgeCheckAngle * Mathf.Deg2Rad;
            Vector2 rayDir = new Vector2(Mathf.Sin(rad) * dir, -Mathf.Cos(rad));
            Gizmos.DrawRay(origin, rayDir * edgeCheckDistance);
        }

        // 绘制墙壁检测射线（水平）
        Gizmos.color = Color.magenta;
        for (int dir = -1; dir <= 1; dir += 2)
        {
            // 修复：X 偏移也需要根据方向调整
            Vector2 origin = (Vector2)transform.position + new Vector2(wallCheckOffset.x * dir, wallCheckOffset.y);
            Gizmos.DrawRay(origin, new Vector2(dir, 0) * wallCheckDistance);
        }
    }
}
