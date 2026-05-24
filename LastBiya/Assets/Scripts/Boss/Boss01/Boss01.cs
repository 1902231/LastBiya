using UnityEngine;
using BehaviorDesigner.Runtime;

/// <summary>
/// Boss 控制器
/// 使用 HFSM 管理状态执行，通过 Behavior Designer 控制状态切换决策
/// </summary>
public class Boss01 : MonoBehaviour, IDamageable, IUnit, IBoss
{
    [Header("基本信息")]
    public string bossName = "Boss01";
    [Header("生命值")]
    public float maxHP = 500;
    public float currentHP;

    [Header("躯干值")]
    [Tooltip("最大躯干值")]
    public float maxPosture = 100;
    [Tooltip("当前躯干值")]
    public float currentPosture;
    [Tooltip("破防时的击退力度")]
    public float stunnedKnockbackForce = 8f;
    [Tooltip("破防击退角度，0 = 正上方，90 = 水平，建议 20~40")]
    [Range(0f, 89f)]
    public float stunnedKnockbackAngle = 30f;

    [Header("移动")]
    public float moveSpeed = 3f;
    [Tooltip("移动时的加速度")]
    public float moveAcceleration = 30f;

    [Header("前劈攻击")]
    [Tooltip("前劈攻击前摇时间")]
    public float slashWindupDuration = 0.8f;
    [Tooltip("前劈攻击判定持续时间")]
    public float slashActiveDuration = 0.3f;
    [Tooltip("前劈攻击后摇时间")]
    public float slashRecoveryDuration = 0.5f;
    [Tooltip("前劈攻击伤害")]
    public float slashDamage = 20f;

    [Header("飞起")]
    [Tooltip("飞起速度")]
    public float flyUpSpeed = 8f;
    [Tooltip("飞到玩家头顶的垂直偏移")]
    public float flyUpOffsetY = 5f;
    [Tooltip("飞到玩家头顶的水平偏移")]
    public float flyUpOffsetX = 0f;
    [Tooltip("到达目标点的判定距离")]
    public float flyUpArriveDistance = 0.5f;
    [Tooltip("飞行高度上限（世界坐标Y值）")]
    public float flyUpMaxHeight = 15f;
    [Tooltip("飞行速度曲线（X=飞行进度0~1，Y=速度倍率）")]
    public AnimationCurve flyUpSpeedCurve = AnimationCurve.Linear(0, 1, 1, 1);

    [Header("下砸")]
    [Tooltip("下砸冲刺速度")]
    public float slamDownSpeed = 20f;
    [Tooltip("下砸伤害")]
    public float slamDownDamage = 30f;
    [Tooltip("下砸到地面后的硬直时间")]
    public float slamDownRecoveryDuration = 0.8f;
    [Tooltip("下砸贴地子弹预制体")]
    public GameObject slamGroundProjectilePrefab;
    [Tooltip("贴地子弹速度")]
    public float slamProjectileSpeed = 12f;
    [Tooltip("贴地子弹伤害")]
    public float slamProjectileDamage = 10f;
    [Tooltip("贴地子弹韧性伤害")]
    public float slamProjectilePostureDamage = 5f;
    [Tooltip("贴地子弹墙壁检测射线长度")]
    public float slamProjectileWallCheckDistance = 0.5f;
    [Tooltip("贴地子弹墙壁检测Layer")]
    public LayerMask slamProjectileWallLayer;

    [Header("远程攻击")]
    [Tooltip("远程攻击前摇时间")]
    public float rangedWindupDuration = 0.6f;
    [Tooltip("远程攻击后摇时间")]
    public float rangedRecoveryDuration = 0.4f;
    [Tooltip("远程攻击弹射物预制体")]
    public GameObject rangedProjectilePrefab;
    [Tooltip("弹射物发射速度")]
    public float projectileSpeed = 10f;
    [Tooltip("弹射物最大飞行距离")]
    public float projectileMaxDistance = 15f;
    [Tooltip("弹射物伤害")]
    public float projectileDamage = 15f;

    [Header("破防倒地")]
    [Tooltip("破防倒地持续时间")]
    public float stunnedDuration = 3f;

    [Header("Hitbox 引用")]
    [Tooltip("前劈攻击 Hitbox（前方横向判定）")]
    public AttackHitbox slashHitbox;
    [Tooltip("下砸攻击 Hitbox（下方纵向判定）")]
    public AttackHitbox slamHitbox;
    [Tooltip("碰撞伤害 Hitbox（持续激活）")]
    public AttackHitbox contactHitbox;

    [Header("碰撞伤害")]
    [Tooltip("碰撞伤害值")]
    public float contactDamage = 10f;

    [Header("受击")]
    [Tooltip("受击击退力度")]
    public float hurtKnockbackForce = 5f;

    [Header("墙壁检测（飞行中）")]
    [Tooltip("墙壁检测射线长度")]
    public float wallCheckDistance = 1f;
    [Tooltip("墙壁/天花板检测 Layer")]
    public LayerMask wallLayer;

    [Header("地面检测")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0, -1f);
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    // 引用
    public Rigidbody2D Rb { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public DamageInfo LastDamageInfo { get; private set; }
    
    // 朝向：1 = 右，-1 = 左
    public int FacingDirection { get; private set; } = 1;
    
    // 地面检测
    public bool IsGrounded { get; private set; }

    // IUnit 实现
    public string UnitName => bossName;
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public float CurrentPosture => currentPosture;
    public float MaxPosture => maxPosture;

    // 状态机
    public HFSM<E_BossStateType_01, Boss01> StateMachine { get; private set; }

    // 当前状态类型（用于 Behavior Designer 查询）
    private E_BossStateType_01 currentStateType;

    // IBoss 实现
    public string BossID => bossName;
    public bool IsDead => currentHP <= 0;
    public Transform Transform => transform;
    
    private BehaviorTree behaviorTree;
    private Animator animator;

    void Awake()
    {
        behaviorTree = GetComponent<BehaviorTree>();
        animator = GetComponent<Animator>();
        
        // 默认禁用行为树，由 BossBattleManager 启动
        if (behaviorTree != null)
            behaviorTree.DisableBehavior();
    }

    void Start()
    {
        Rb = GetComponent<Rigidbody2D>();
        currentHP = maxHP;
        currentPosture = maxPosture;

        // 玩家引用将在战斗开始时获取（由 BossBattleManager 触发）

        // 初始化状态机
        StateMachine = new HFSM<E_BossStateType_01, Boss01>(this);

        StateMachine.AddState(E_BossStateType_01.Alive,         new Boss_AliveState());
        StateMachine.AddState(E_BossStateType_01.Idle,          new Boss_IdleState());
        StateMachine.AddState(E_BossStateType_01.Move,          new Boss_MoveState());
        StateMachine.AddState(E_BossStateType_01.SlashAttack,   new Boss_SlashAttackState());
        StateMachine.AddState(E_BossStateType_01.FlyUp,         new Boss_FlyUpState());
        StateMachine.AddState(E_BossStateType_01.SlamDown,      new Boss_SlamDownState());
        StateMachine.AddState(E_BossStateType_01.RangedAttack,  new Boss_RangedAttackState());
        StateMachine.AddState(E_BossStateType_01.Stunned,       new Boss_StunnedState());
        StateMachine.AddState(E_BossStateType_01.Dead,          new Boss_DeadState());

        StateMachine.SwitchState(E_BossStateType_01.Alive);

        // 碰撞伤害 hitbox 持续激活
        if (contactHitbox != null)
        {
            contactHitbox.damage = (int)contactDamage;
            contactHitbox.gameObject.SetActive(true);
        }
    }
    
    // ========== IBoss 接口实现 ==========
    
    /// <summary>
    /// 启动 Boss 战斗逻辑（启动行为树）
    /// </summary>
    public void StartCombat()
    {
        // 在战斗开始时查找玩家（确保玩家已激活）
        if (PlayerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerTransform = player.transform;
                Debug.Log($"✅ [{bossName}] 在战斗开始时找到玩家");
            }
            else
            {
                Debug.LogError($"❌ [{bossName}] 无法找到玩家！请确保玩家有 'Player' Tag");
            }
        }

        if (behaviorTree != null)
        {
            behaviorTree.EnableBehavior();
            Debug.Log($"[{bossName}] 行为树已启动");
        }
        else
        {
            Debug.LogWarning($"[{bossName}] 未找到 Behavior Tree 组件！");
        }
    }
    
    /// <summary>
    /// 设置玩家引用（由 BossBattleManager 调用）
    /// </summary>
    /// <param name="player">玩家 Transform</param>
    public void SetPlayer(Transform player)
    {
        PlayerTransform = player;
        Debug.Log($"✅ [{bossName}] 通过 BossBattleManager 获取到玩家");
    }
    
    /// <summary>
    /// 停止 Boss 战斗逻辑（停止行为树）
    /// </summary>
    public void StopCombat()
    {
        if (behaviorTree != null)
        {
            behaviorTree.DisableBehavior();
            Debug.Log($"[{bossName}] 行为树已停止");
        }
    }
    
    /// <summary>
    /// 播放开场动画
    /// </summary>
    /// <returns>动画时长（秒）</returns>
    public float PlayIntroAnimation()
    {
        if (animator != null)
        {
            // TODO: 替换为实际的开场动画名称
            // animator.Play("Boss_Intro");
            Debug.Log($"[{bossName}] 播放开场动画");
            return 3f; // 返回动画时长
        }
        return 0f;
    }

    void Update()
    {
        // 地面检测
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        IsGrounded = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundLayer);

        StateMachine.OnUpdate();
    }

    void FixedUpdate()
    {
        StateMachine.OnFixedUpdate();
    }

    // ========== 公共方法（供 Behavior Designer 调用）==========

    /// <summary>
    /// 切换到指定状态（供 Behavior Designer 调用）
    /// </summary>
    public void SwitchToState(E_BossStateType_01 stateType)
    {
        currentStateType = stateType;
        StateMachine.SwitchState(stateType);
    }

    /// <summary>
    /// 获取当前状态类型
    /// </summary>
    public E_BossStateType_01 GetCurrentState()
    {
        return currentStateType;
    }

    /// <summary>
    /// 获取血量百分比（0-1）
    /// </summary>
    public float GetHPPercentage()
    {
        return currentHP / maxHP;
    }

    /// <summary>
    /// 获取与玩家的距离
    /// </summary>
    public float GetDistanceToPlayer()
    {
        if (PlayerTransform == null) return float.MaxValue;
        return Vector2.Distance(transform.position, PlayerTransform.position);
    }

    /// <summary>
    /// 玩家是否在 Boss 前方
    /// </summary>
    public bool IsPlayerInFront()
    {
        if (PlayerTransform == null) return false;
        float dirToPlayer = PlayerTransform.position.x - transform.position.x;
        return Mathf.Sign(dirToPlayer) == FacingDirection;
    }

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
    /// 朝向玩家
    /// </summary>
    public void FacePlayer()
    {
        if (PlayerTransform == null) return;
        int dir = PlayerTransform.position.x > transform.position.x ? 1 : -1;
        UpdateFacing(dir);
    }

    /// <summary>
    /// 用力驱动水平移动（在 FixedUpdate 中调用）
    /// </summary>
    public void ApplyHorizontalMovement(float inputX)
    {
        float targetSpeed = inputX * moveSpeed;
        float speedDiff = targetSpeed - Rb.velocity.x;
        Rb.AddForce(Vector2.right * speedDiff * moveAcceleration, ForceMode2D.Force);
    }

    /// <summary>
    /// 停止水平移动，保留垂直速度
    /// </summary>
    public void StopHorizontalMovement()
    {
        Rb.velocity = new Vector2(0, Rb.velocity.y);
    }

    /// <summary>
    /// 检测飞行方向上是否有墙壁
    /// 沿移动方向发射一条主射线，再根据移动方向的分量发射辅助射线
    /// </summary>
    public bool CheckWallInDirection(Vector2 moveDirection)
    {
        Vector2 origin = transform.position;
        Vector2 dir = moveDirection.normalized;

        // 主射线：沿移动方向
        if (Physics2D.Raycast(origin, dir, wallCheckDistance, wallLayer))
            return true;

        // 辅助射线：只在移动方向有明显水平分量时检测对应侧面
        if (dir.x > 0.1f && Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, wallLayer))
            return true;
        if (dir.x < -0.1f && Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, wallLayer))
            return true;

        // 只在移动方向有明显垂直分量时检测上下
        if (dir.y > 0.1f && Physics2D.Raycast(origin, Vector2.up, wallCheckDistance, wallLayer))
            return true;
        if (dir.y < -0.1f && Physics2D.Raycast(origin, Vector2.down, wallCheckDistance, wallLayer))
            return true;

        return false;
    }

    /// <summary>
    /// 获取飞到玩家头顶的目标位置
    /// </summary>
    public Vector2 GetFlyUpTargetPosition()
    {
        if (PlayerTransform == null) return transform.position;
        return (Vector2)PlayerTransform.position + new Vector2(flyUpOffsetX, flyUpOffsetY);
    }

    /// <summary>
    /// 获取躯干值百分比（0-1）
    /// </summary>
    public float GetPosturePercentage()
    {
        return currentPosture / maxPosture;
    }

    /// <summary>
    /// 韧性是否被锁定（破防期间不可被攻击降低）
    /// </summary>
    public bool IsPostureLocked { get; set; }

    /// <summary>
    /// 对躯干值造成伤害，返回是否破防
    /// </summary>
    public bool DamagePosture(float amount)
    {
        if (IsPostureBroken || IsPostureLocked) return false;
        currentPosture -= amount;
        if (currentPosture <= 0)
        {
            currentPosture = 0;
            EventCenter.Instance.EventTrigger<IUnit>("PostureChanged", this);
            return true;
        }
        EventCenter.Instance.EventTrigger<IUnit>("PostureChanged", this);
        return false;
    }

    /// <summary>
    /// 躯干值是否已归零
    /// </summary>
    public bool IsPostureBroken => currentPosture <= 0;

    /// <summary>
    /// 当前状态的行为是否已完成（供 Behavior Designer 查询）
    /// </summary>
    public bool IsCurrentStateComplete()
    {
        var current = StateMachine.currentState;
        return current switch
        {
            Boss_SlashAttackState s  => s.IsAttackComplete(),
            Boss_RangedAttackState r => r.IsAttackComplete(),
            Boss_FlyUpState f        => f.HasReachedTarget(),
            Boss_SlamDownState sd    => sd.IsSlamComplete(),
            Boss_StunnedState st     => st.IsStunComplete(),
            // Idle、Move 等持续性状态没有"完成"概念，由行为树主动切走
            _ => false
        };
    }

    /// <summary>
    /// IDamageable 实现
    /// </summary>
    public void TakeDamage(DamageInfo info)
    {
        currentHP -= info.damage;
        LastDamageInfo = info;

        EventCenter.Instance.EventTrigger<IUnit>("HPChanged", this);

        // 韧性伤害
        if (info.postureDamage > 0)
            DamagePosture(info.postureDamage);

        if (currentHP <= 0)
        {
            // 测试用：注释掉死亡切换
            // StateMachine.SwitchState(E_BossStateType_01.Dead);
            // currentStateType = E_BossStateType_01.Dead;
        }
    }

    void OnDrawGizmosSelected()
    {
        // 地面检测
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);

        // 飞起目标位置
        if (PlayerTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(GetFlyUpTargetPosition(), 0.3f);
        }

        // 飞行高度上限（黄色水平线）
        Gizmos.color = Color.yellow;
        Vector3 leftPoint = new Vector3(transform.position.x - 10f, flyUpMaxHeight, 0f);
        Vector3 rightPoint = new Vector3(transform.position.x + 10f, flyUpMaxHeight, 0f);
        Gizmos.DrawLine(leftPoint, rightPoint);

        // 墙壁检测射线（显示所有可能方向）
        Vector2 origin = transform.position;
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(origin, Vector2.up * wallCheckDistance);
        Gizmos.DrawRay(origin, Vector2.down * wallCheckDistance);
        Gizmos.DrawRay(origin, Vector2.left * wallCheckDistance);
        Gizmos.DrawRay(origin, Vector2.right * wallCheckDistance);
    }
}
