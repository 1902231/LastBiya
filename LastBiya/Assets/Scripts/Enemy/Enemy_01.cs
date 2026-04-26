using UnityEngine;

/// <summary>
/// Enemy_01 控制器
/// 基础敌人：在平台上巡逻，发现玩家后追逐并攻击，可被击退
/// </summary>
public class Enemy_01 : MonoBehaviour, IDamageable
{
    [Header("生命值")]
    public float maxHP = 30;
    public float currentHP;

    [Header("移动")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("攻击")]
    public float attackRange = 1.5f;
    public float attackDuration = 0.4f;
    public float attackCooldown = 1f;
    [HideInInspector] public float attackCooldownTimer;
    [Tooltip("攻击判定 Hitbox")]
    public AttackHitbox attackHitbox;

    [Header("受击")]
    public float hurtDuration = 0.3f;
    public float hurtLaunchUpForce = 3f;

    [Header("检测")]
    public float detectRange = 6f;
    public LayerMask groundLayer;
    public LayerMask playerLayer;

    [Header("平台边缘检测（射线）")]
    public Vector2 edgeCheckOffset = new Vector2(0.5f, 0f);
    public float edgeCheckDistance = 1.5f;
    public float edgeCheckAngle = 45f;

    [Header("墙壁检测（水平射线）")]
    [Tooltip("射线发射点相对敌人中心的偏移（y 控制高度，建议略高于脚底）")]
    public Vector2 wallCheckOffset = new Vector2(0f, -0.2f);
    public float wallCheckDistance = 0.5f;

    // 引用
    public Rigidbody2D Rb { get; private set; }
    public Transform PlayerTransform { get; private set; }

    // 朝向：1 = 右，-1 = 左
    public int FacingDirection { get; private set; } = 1;

    // 受击信息
    public DamageInfo LastDamageInfo { get; private set; }

    // 状态机
    public HFSM<E_Enemy01StateType, Enemy_01> StateMachine { get; private set; }

    void Start()
    {
        Rb = GetComponent<Rigidbody2D>();
        currentHP = maxHP;

        // 查找玩家
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) PlayerTransform = player.transform;

        // 初始化状态机
        StateMachine = new HFSM<E_Enemy01StateType, Enemy_01>(this);

        StateMachine.AddState(E_Enemy01StateType.Alive,   new Enemy_01_AliveState());
        StateMachine.AddState(E_Enemy01StateType.Patrol,  new Enemy_01_PatrolState());
        StateMachine.AddState(E_Enemy01StateType.Chase,   new Enemy_01_ChaseState());
        StateMachine.AddState(E_Enemy01StateType.Attack,  new Enemy_01_AttackState());
        StateMachine.AddState(E_Enemy01StateType.Hurt,    new Enemy_01_HurtState());
        StateMachine.AddState(E_Enemy01StateType.Dead,    new Enemy_01_DeadState());

        StateMachine.SwitchState(E_Enemy01StateType.Patrol);
    }

    void Update()
    {
        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        StateMachine.OnUpdate();
    }

    void FixedUpdate()
    {
        StateMachine.OnFixedUpdate();
    }

    // ========== 公共方法 ==========

    /// <summary>
    /// 更新朝向
    /// </summary>
    public void UpdateFacing(int dir)
    {
        if (dir == 0) return;
        FacingDirection = dir > 0 ? 1 : -1;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * FacingDirection;
        transform.localScale = scale;
    }

    /// <summary>
    /// 检测玩家是否在感知范围内
    /// </summary>
    public bool DetectPlayer()
    {
        if (PlayerTransform == null) return false;
        return Vector2.Distance(transform.position, PlayerTransform.position) <= detectRange;
    }

    /// <summary>
    /// 检测玩家是否在攻击范围内
    /// </summary>
    public bool IsInAttackRange()
    {
        if (PlayerTransform == null) return false;
        return Vector2.Distance(transform.position, PlayerTransform.position) <= attackRange;
    }

    /// <summary>
    /// 检测指定方向前方是否还有平台（斜下方射线）
    /// </summary>
    public bool HasGroundAhead(int direction)
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(edgeCheckOffset.x * direction, edgeCheckOffset.y);
        float angle = edgeCheckAngle * Mathf.Deg2Rad;
        Vector2 rayDir = new Vector2(Mathf.Sin(angle) * direction, -Mathf.Cos(angle));
        RaycastHit2D hit = Physics2D.Raycast(origin, rayDir, edgeCheckDistance, groundLayer);
        return hit.collider != null;
    }

    /// <summary>
    /// 检测前方是否有墙壁（水平射线）
    /// </summary>
    public bool HasWallAhead(int direction)
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(wallCheckOffset.x, wallCheckOffset.y);
        Vector2 rayDir = new Vector2(direction, 0);
        return Physics2D.Raycast(origin, rayDir, wallCheckDistance, groundLayer);
    }

    /// <summary>
    /// IDamageable 实现
    /// </summary>
    public void TakeDamage(DamageInfo info)
    {
        currentHP -= info.damage;
        LastDamageInfo = info;

        if (currentHP <= 0)
            StateMachine.SwitchState(E_Enemy01StateType.Dead);
        else
            StateMachine.SwitchState(E_Enemy01StateType.Hurt);
    }

    void OnDrawGizmosSelected()
    {
        // 感知范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // 攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 平台边缘射线
        Gizmos.color = Color.cyan;
        for (int dir = -1; dir <= 1; dir += 2)
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(edgeCheckOffset.x * dir, edgeCheckOffset.y);
            float angle = edgeCheckAngle * Mathf.Deg2Rad;
            Vector2 rayDir = new Vector2(Mathf.Sin(angle) * dir, -Mathf.Cos(angle));
            Gizmos.DrawRay(origin, rayDir * edgeCheckDistance);
        }

        // 墙壁检测射线
        Gizmos.color = Color.magenta;
        for (int dir = -1; dir <= 1; dir += 2)
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(wallCheckOffset.x, wallCheckOffset.y);
            Gizmos.DrawRay(origin, new Vector2(dir, 0) * wallCheckDistance);
        }
    }
}
