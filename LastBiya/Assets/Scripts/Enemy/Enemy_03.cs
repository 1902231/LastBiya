using UnityEngine;

/// <summary>
/// Enemy_03：空中追踪单位
/// 巡逻时随机漂移，发现玩家后跟踪玩家上方锚点，间歇性俯冲攻击
/// </summary>
public class Enemy_03 : MonoBehaviour, IDamageable
{
    [Header("生命值")]
    public float maxHP = 20;
    public float currentHP;

    [Header("移动")]
    [Tooltip("巡逻时随机漂移速度")]
    public float patrolSpeed = 1.5f;
    [Tooltip("跟踪锚点时的移动速度")]
    public float trackingSpeed = 4f;
    [Tooltip("俯冲攻击速度")]
    public float diveSpeed = 12f;

    [Header("检测")]
    [Tooltip("巡逻时发现玩家的检测范围")]
    public float patrolDetectRange = 8f;
    [Tooltip("追击时丢失玩家的检测范围（通常大于巡逻检测范围，避免频繁切换）")]
    public float trackingDetectRange = 12f;

    [Header("跟踪锚点")]
    [Tooltip("锚点相对玩家的水平偏移")]
    public float anchorOffsetX = 3f;
    [Tooltip("锚点相对玩家的垂直偏移")]
    public float anchorOffsetY = 3f;
    [Tooltip("到达锚点的判定距离")]
    public float anchorArriveDistance = 0.5f;

    [Header("俯冲攻击")]
    [Tooltip("跟踪多久后发起俯冲")]
    public float trackingDuration = 2f;
    [Tooltip("俯冲持续时间（超时自动结束）")]
    public float diveDuration = 0.6f;
    [Tooltip("俯冲命中判定距离（到达玩家附近视为结束）")]
    public float diveArriveDistance = 1f;

    [Header("受击")]
    public float hurtDuration = 0.3f;
    public float hurtKnockbackForce = 5f;

    // 引用
    public Rigidbody2D Rb { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public DamageInfo LastDamageInfo { get; private set; }
    public int FacingDirection { get; private set; } = 1;

    public HFSM<E_Enemy03StateType, Enemy_03> StateMachine { get; private set; }

    void Start()
    {
        Rb = GetComponent<Rigidbody2D>();
        Rb.gravityScale = 0f; // 空中单位不受重力
        currentHP = maxHP;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) PlayerTransform = player.transform;

        StateMachine = new HFSM<E_Enemy03StateType, Enemy_03>(this);

        StateMachine.AddState(E_Enemy03StateType.Alive,      new Enemy_03_AliveState());
        StateMachine.AddState(E_Enemy03StateType.Patrol,     new Enemy_03_PatrolState());
        StateMachine.AddState(E_Enemy03StateType.Tracking,   new Enemy_03_TrackingState());
        StateMachine.AddState(E_Enemy03StateType.DiveAttack, new Enemy_03_DiveAttackState());
        StateMachine.AddState(E_Enemy03StateType.Hurt,       new Enemy_03_HurtState());
        StateMachine.AddState(E_Enemy03StateType.Dead,       new Enemy_03_DeadState());

        StateMachine.SwitchState(E_Enemy03StateType.Alive);
    }

    void Update()
    {
        StateMachine.OnUpdate();
    }

    void FixedUpdate()
    {
        StateMachine.OnFixedUpdate();
    }

    public bool DetectPlayer(float range)
    {
        if (PlayerTransform == null) return false;
        return Vector2.Distance(transform.position, PlayerTransform.position) <= range;
    }

    public void UpdateFacing(float targetX)
    {
        if (Mathf.Abs(targetX - transform.position.x) < 0.1f) return;
        FacingDirection = targetX > transform.position.x ? 1 : -1;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * FacingDirection;
        transform.localScale = scale;
    }

    /// <summary>
    /// 获取距离自身更近的那个锚点（玩家左上或右上）
    /// </summary>
    public Vector2 GetCloserAnchorPoint()
    {
        if (PlayerTransform == null) return transform.position;

        Vector2 playerPos = PlayerTransform.position;
        Vector2 leftAnchor = playerPos + new Vector2(-anchorOffsetX, anchorOffsetY);
        Vector2 rightAnchor = playerPos + new Vector2(anchorOffsetX, anchorOffsetY);

        float distLeft = Vector2.Distance(transform.position, leftAnchor);
        float distRight = Vector2.Distance(transform.position, rightAnchor);

        return distLeft <= distRight ? leftAnchor : rightAnchor;
    }

    public void TakeDamage(DamageInfo info)
    {
        currentHP -= info.damage;
        LastDamageInfo = info;

        if (currentHP <= 0)
            StateMachine.SwitchState(E_Enemy03StateType.Dead);
        else
            StateMachine.SwitchState(E_Enemy03StateType.Hurt);
    }

    void OnDrawGizmosSelected()
    {
        // 巡逻检测范围（黄色）
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, patrolDetectRange);

        // 追击检测范围（橙色）
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, trackingDetectRange);

        // 画锚点位置
        if (PlayerTransform != null)
        {
            Vector2 playerPos = PlayerTransform.position;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerPos + new Vector2(-anchorOffsetX, anchorOffsetY), 0.2f);
            Gizmos.DrawWireSphere(playerPos + new Vector2(anchorOffsetX, anchorOffsetY), 0.2f);
        }
    }
}
