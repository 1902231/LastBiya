using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_PlayerStateType
{
    //父状态
    Alive,
    Grounded,
    AirBornd,
    //子状态
    Idle,
    Move,
    Jump,
    DoubleJump,
    FreeFall,
    FallingDash,
    //独立状态
    Dash,
    DashBounce,
    FallingDashSlide,
    Heart,
}

public enum E_PlayerAbilityType
{ 
    DoubleJump,
    Dash,
    FallingDash,
    Attack,
    ChargeAttack,
    Invincible,
}

public class PlayerController : MonoBehaviour, IDamageable, IUnit
{
    [Header("玩家战斗属性")]

    public float maxHP = 100;
    public float currentHP;
    public float damage = 10;

    [Tooltip("受伤时 TimeScale 缩放到的值，0 = 完全冻结，0.1 = 极慢")]
    [Range(0f, 1f)]
    public float HurtTimeScale = 0.05f;

    [Tooltip("受伤击退时额外的向上力度")]
    public float HurtLaunchUpForce = 5f;

    [Tooltip("受伤顿帧时长（现实秒数）")]
    public float HitStopDuration = 0.4f;

    [Tooltip("击退硬直时长（游戏秒数）")]
    public float KnockbackDuration = 0.3f;

    [Tooltip("受伤后无敌持续时间")]
    public float HurtInvincibleDuration = 1f;

    [Header("攻击属性")]
    [Tooltip("攻击判定帧持续时间")]
    public float AttackActiveDuration = 0.15f;
    [Tooltip("攻击后摇时间")]
    public float AttackRecoveryDuration = 0.2f;
    [Tooltip("普通攻击韧性伤害")]
    public float AttackPostureDamage = 5f;
    [Tooltip("普通攻击命中回血")]
    public float AttackLifeSteal = 2f;

    [Header("蓄力攻击属性")]
    [Tooltip("蓄力所需时间")]
    public float ChargeTime = 1f;
    [Tooltip("蓄力释放后 Hitbox 持续时间")]
    public float ChargeReleaseDuration = 0.2f;
    [Tooltip("蓄力攻击伤害")]
    public float ChargeDamage = 30;
    [Tooltip("蓄力攻击韧性伤害")]
    public float ChargePostureDamage = 20f;
    [Tooltip("蓄力攻击命中回血")]
    public float ChargeLifeSteal = 5f;

    /// <summary>
    /// 最近一次受伤信息，供 Heart 状态读取
    /// </summary>
    public DamageInfo LastDamageInfo { get; private set; }

    [Header("玩家移动属性")]
    public float MoveSpeed = 5;
    [Tooltip("水平加速力度，值越大响应越快")]
    public float MoveAcceleration = 50f;
    public float JumpForce = 10;
    [Tooltip("松开跳跃键时，垂直速度乘以该系数（0~1，越小截断越狠）")]
    public float JumpCutMultiplier = 0.5f;
    public bool isGrounded;

    [Header("冲刺")]
    public float DashSpeed = 20f;
    public float DashDuration = 0.15f;
    public float DashCooldown = 1f;
    private float dashCooldownTimer;

    [Tooltip("冲刺命中敌人时的反弹力度")]
    public float DashBounceForce = 8f;
    [Tooltip("冲刺反弹角度，0 = 正上方，90 = 水平反方向，建议 20~45")]
    [Range(0f, 89f)]
    public float DashBounceAngle = 30f;
    [Tooltip("冲刺韧性伤害")]
    public float DashPostureDamage = 3f;
    [Tooltip("冲刺命中回血")]
    public float DashLifeSteal = 1f;
    [Tooltip("冲刺反弹无敌持续时间")]
    public float DashBounceDuration = 0.3f;
    [Tooltip("冲刺反弹期间输入叠加的水平力度")]
    public float DashBounceAirControl = 20f;

    [Header("下落冲刺")]
    public float FallingDashSpeed = 30f;
    [Tooltip("下冲前摇时间")]
    public float FallingDashWindupDuration = 0.2f;
    [Tooltip("下冲角度，0 = 正下方，90 = 水平，建议 30~60")]
    [Range(0f, 89f)]
    public float FallingDashAngle = 45f;
    [Tooltip("下冲伤害")]
    public float FallingDashDamage = 15;
    [Tooltip("下冲韧性伤害")]
    public float FallingDashPostureDamage = 10f;
    [Tooltip("下冲命中回血")]
    public float FallingDashLifeSteal = 3f;
    [Tooltip("下冲最大持续时间")]
    public float FallingDashMaxDuration = 0.8f;
    [Tooltip("下冲二段水平滑行力度")]
    public float FallingDashSlideForce = 15f;
    [Tooltip("下冲二段持续时间")]
    public float FallingDashSlideDuration = 0.3f;

    [Header("土狼时间")]
    public float CoyoteTime = 0.12f;

    [Header("能力解锁")]
    [Tooltip("勾选启用二段跳能力")]
    public bool DoubleJumpUnlocked = true;
    [Tooltip("勾选启用冲刺能力")]
    public bool DashUnlocked = true;
    [Tooltip("勾选启用下冲能力")]
    public bool FallingDashUnlocked = true;
    [Tooltip("勾选启用攻击能力")]
    public bool AttackUnlocked = true;
    [Tooltip("勾选启用蓄力攻击能力")]
    public bool ChargeAttackUnlocked = true;

    /// <summary>
    /// 冲刺是否可用
    /// </summary>
    public bool CanDash() => dashCooldownTimer <= 0;

    /// <summary>
    /// 冲刺时调用，开始计时 CD
    /// </summary>
    public void StartDashCooldown() => dashCooldownTimer = DashCooldown;

    [Header("地面检测")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0, -0.5f);
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("墙壁检测")]
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private LayerMask wallLayer;


    public Rigidbody2D Rb { get; private set; }

    // IUnit 实现
    public string UnitName => "Player";
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public float CurrentPosture => 0f;
    public float MaxPosture => 0f;

    /// <summary>
    /// 玩家当前朝向，1 = 右，-1 = 左
    /// </summary>
    public int FacingDirection { get; private set; } = 1;

    private HFSM<E_PlayerStateType, PlayerController> playerFsm;
    public AbilityManager<E_PlayerAbilityType, PlayerController> AbilityMgr { get; private set; }
    
    
    //[SerializeField]private PlayerInputHandler inputHandler;
    //public PlayerInputHandler Input => inputHandler;

    void Start()
    {
        Rb = GetComponent<Rigidbody2D>();
        currentHP = maxHP;


        playerFsm = new HFSM<E_PlayerStateType, PlayerController>(this);
        AbilityMgr = new AbilityManager<E_PlayerAbilityType, PlayerController>(this);

        // 注册能力
        var doubleJump = new PlayerAbility_DoubleJump(1);
        doubleJump.isUnlocked = DoubleJumpUnlocked;
        AbilityMgr.AddAbilities(E_PlayerAbilityType.DoubleJump, doubleJump);

        var dash = new PlayerAbility_Dash();
        dash.isUnlocked = DashUnlocked;
        AbilityMgr.AddAbilities(E_PlayerAbilityType.Dash, dash);

        var fallingDash = new PlayerAbility_FallingDash();
        fallingDash.isUnlocked = FallingDashUnlocked;
        AbilityMgr.AddAbilities(E_PlayerAbilityType.FallingDash, fallingDash);

        var attack = new PlayerAbility_Attack();
        attack.isUnlocked = AttackUnlocked;
        attack.activeDuration = AttackActiveDuration;
        attack.recoveryDuration = AttackRecoveryDuration;
        AbilityMgr.AddAbilities(E_PlayerAbilityType.Attack, attack);
        attack.FindHitboxes(); // 自动从子物体查找 Hitbox

        var chargeAttack = new PlayerAbility_ChargeAttack();
        chargeAttack.isUnlocked = ChargeAttackUnlocked;
        AbilityMgr.AddAbilities(E_PlayerAbilityType.ChargeAttack, chargeAttack);

        var invincible = new PlayerAbility_Invincible();
        invincible.isUnlocked = true;
        AbilityMgr.AddAbilities(E_PlayerAbilityType.Invincible, invincible);


        playerFsm.AddState(E_PlayerStateType.Alive, new PlayerState_Alive());

        playerFsm.AddState(E_PlayerStateType.Grounded, new PlayerState_Grounded());
        playerFsm.AddState(E_PlayerStateType.Idle, new PlayerState_Idle());
        playerFsm.AddState(E_PlayerStateType.Move, new PlayerState_Move());

        playerFsm.AddState(E_PlayerStateType.AirBornd, new PlayerState_AirBornd());
        playerFsm.AddState(E_PlayerStateType.Jump, new PlayerState_Jump());
        playerFsm.AddState(E_PlayerStateType.DoubleJump, new PlayerState_DoubleJump());
        playerFsm.AddState(E_PlayerStateType.FreeFall, new PlayerState_FreeFall());

        playerFsm.AddState(E_PlayerStateType.Dash, new PlayerState_Dash());
        playerFsm.AddState(E_PlayerStateType.DashBounce, new PlayerState_DashBounce());
        playerFsm.AddState(E_PlayerStateType.FallingDash, new PlayerState_FallingDash());
        playerFsm.AddState(E_PlayerStateType.FallingDashSlide, new PlayerState_FallingDashSlide());

        playerFsm.AddState(E_PlayerStateType.Heart, new PlayerState_Heart());

        playerFsm.SwitchState(E_PlayerStateType.Idle);

    }

    // Update is called once per frame
    void Update()
    {
        //地面检测
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        isGrounded = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundLayer);

        //冲刺 CD 倒计时
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;

        // 同步 Inspector 上的能力解锁开关到 Ability 实例
        var dj = AbilityMgr.Get(E_PlayerAbilityType.DoubleJump);
        if (dj != null) dj.isUnlocked = DoubleJumpUnlocked;
        var da = AbilityMgr.Get(E_PlayerAbilityType.Dash);
        if (da != null) da.isUnlocked = DashUnlocked;
        var fd = AbilityMgr.Get(E_PlayerAbilityType.FallingDash);
        if (fd != null) fd.isUnlocked = FallingDashUnlocked;
        var atk = AbilityMgr.Get(E_PlayerAbilityType.Attack);
        if (atk != null) atk.isUnlocked = AttackUnlocked;
        var ca = AbilityMgr.Get(E_PlayerAbilityType.ChargeAttack);
        if (ca != null) ca.isUnlocked = ChargeAttackUnlocked;

        //状态机运行
        playerFsm.OnUpdate();

        // 驱动能力系统（在 HFSM 之后，确保 Ability 动画覆盖 HFSM 动画）
        AbilityMgr.Tick(Time.deltaTime);

        Debug.Log("玩家当前状态：" + playerFsm.currentState);

        Debug.Log($"fixedDeltaTime: {Time.fixedDeltaTime}, timeScale: {Time.timeScale}");
    }

    private void FixedUpdate()
    {
        playerFsm.OnFixedUpdate();
    }


    /// <summary>
    /// 根据水平输入更新朝向，输入为 0 时不改变
    /// </summary>
    public void UpdateFacing(float inputX)
    {
        if (inputX == 0) return;
        FacingDirection = inputX > 0 ? 1 : -1;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * FacingDirection;
        transform.localScale = scale;
    }

    /// <summary>
    /// 是否有 Ability 锁定了移动（蓄力攻击等）
    /// </summary>
    public bool IsMovementLocked => AbilityMgr.IsActive(E_PlayerAbilityType.ChargeAttack);

    /// <summary>
    /// 检测面朝方向是否有墙壁
    /// </summary>
    public bool IsWallAhead()
    {
        Vector2 origin = transform.position;
        return Physics2D.Raycast(origin, Vector2.right * FacingDirection, wallCheckDistance, wallLayer);
    }

    /// <summary>
    /// 检测左右两侧是否有墙壁
    /// </summary>
    public bool IsWallOnEitherSide()
    {
        Vector2 origin = transform.position;
        if (Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, wallLayer))
            return true;
        if (Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, wallLayer))
            return true;
        return false;
    }

    /// <summary>
    /// 用力驱动水平移动，在 FixedUpdate 中调用
    /// 计算目标速度与当前速度的差值，施加对应的力
    /// </summary>
    public void ApplyHorizontalMovement(float inputX)
    {
        if (IsMovementLocked) return;
        float targetSpeed = inputX * MoveSpeed;
        float speedDiff = targetSpeed - Rb.velocity.x;
        Rb.AddForce(Vector2.right * speedDiff * MoveAcceleration, ForceMode2D.Force);
    }



    /// <summary>
    /// 是否处于无敌状态（下冲等），免疫敌人攻击但不免疫陷阱
    /// </summary>
    public bool IsInvincible { get; set; }

    /// <summary>
    /// 回血
    /// </summary>
    public void Heal(float amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        EventCenter.Instance.EventTrigger<IUnit>("HPChanged", this);
    }

    /// <summary>
    /// IDamageable 实现：受到伤害时调用
    /// 扣血 → 存储伤害信息 → 通过事件中心通知状态机
    /// </summary>
    public void TakeDamage(DamageInfo info)
    {
        // 无敌期间免疫敌人攻击
        if (IsInvincible && info.source == DamageSource.Enemy)
            return;

        currentHP -= info.damage;
        LastDamageInfo = info;
        EventCenter.Instance.EventTrigger<IUnit>("HPChanged", this);
        EventCenter.Instance.EventTrigger<DamageInfo>("PlayerHurt", info);
    }

    //测试方法,用来绘制碰撞箱
    void OnDrawGizmosSelected()
    {
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
    }
}
