using System.Collections.Generic;
using UnityEngine;

public enum E_BookStateType
{
    Normal,
    Fighting,
}

public enum E_BookAbilityType
{
    RuneAttack,
}

public class BookCotroller : MonoBehaviour
{
    [Header("跟随")]
    [Tooltip("相对玩家的锚点偏移（X 会根据玩家朝向取反）")]
    public Vector2 anchorOffset = new Vector2(-1f, 1.5f);
    [Tooltip("跟随移动速度")]
    public float followSpeed = 4f;
    [Tooltip("跟随平滑度（越小越平滑）")]
    public float followSmoothTime = 0.1f;

    [Header("战斗")]
    [Tooltip("追踪符文伤害")]
    public float runeDamage = 5f;
    [Tooltip("追踪符文韧性伤害")]
    public float runePostureDamage = 3f;
    [Tooltip("符文飞向悬停点的速度")]
    public float runeHoverSpeed = 8f;
    [Tooltip("符文追踪敌人的初始速度")]
    public float runeTrackSpeed = 14f;
    [Tooltip("符文追踪加速度")]
    public float runeTrackAcceleration = 20f;
    [Tooltip("符文在悬停点停顿时间")]
    public float runePauseDuration = 0.3f;
    [Tooltip("符文随机出现范围（相对书本位置）")]
    public float runeSpawnRange = 1f;
    [Tooltip("符文预制体")]
    public GameObject runePrefab;
    [Tooltip("进入Fighting后多久没收到新目标就回Normal")]
    public float fightingTimeout = 2f;
    [Tooltip("符文发射间隔")]
    public float runeFireInterval = 0.15f;

    [Header("特效")]
    [Tooltip("符文命中特效")]
    public GameObject runeHitEffect;
    [Tooltip("符文消失特效")]
    public GameObject runeVanishEffect;

    // 引用
    public Transform PlayerTransform { get; private set; }
    public PlayerController Player { get; private set; }

    // 目标队列
    public Queue<Transform> TargetQueue { get; private set; } = new();

    // 系统
    public HFSM<E_BookStateType, BookCotroller> StateMachine { get; private set; }
    public AbilityManager<E_BookAbilityType, BookCotroller> AbilityMgr { get; private set; }

    // 平滑移动用
    private Vector2 velocity;

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerTransform = playerObj.transform;
            Player = playerObj.GetComponent<PlayerController>();
        }

        StateMachine = new HFSM<E_BookStateType, BookCotroller>(this);
        AbilityMgr = new AbilityManager<E_BookAbilityType, BookCotroller>(this);

        StateMachine.AddState(E_BookStateType.Normal, new BookState_Normal());
        StateMachine.AddState(E_BookStateType.Fighting, new BookState_Fighting());

        var runeAttack = new BookAbility_RuneAttack();
        runeAttack.isUnlocked = true;
        AbilityMgr.AddAbilities(E_BookAbilityType.RuneAttack, runeAttack);

        StateMachine.SwitchState(E_BookStateType.Normal);
    }

    void Update()
    {
        StateMachine.OnUpdate();
        AbilityMgr.Tick(Time.deltaTime);
    }

    public void EnqueueTarget(Transform target)
    {
        if (target != null)
            TargetQueue.Enqueue(target);
    }

    /// <summary>
    /// 获取跟随锚点（X 根据玩家朝向取反，始终在玩家身后）
    /// </summary>
    public Vector2 GetAnchorPosition()
    {
        if (PlayerTransform == null) return transform.position;
        int facing = Player != null ? Player.FacingDirection : 1;
        // 锚点 X 和玩家朝向相反
        float offsetX = -Mathf.Abs(anchorOffset.x) * facing;
        return (Vector2)PlayerTransform.position + new Vector2(offsetX, anchorOffset.y);
    }

    /// <summary>
    /// 平滑移动到锚点
    /// </summary>
    public void MoveTowardsAnchor()
    {
        Vector2 target = GetAnchorPosition();
        Vector2 current = transform.position;
        transform.position = Vector2.SmoothDamp(current, target, ref velocity, followSmoothTime, followSpeed);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (PlayerTransform != null)
        {
            Gizmos.DrawWireSphere(GetAnchorPosition(), 0.2f);
        }
        else
        {
            // 编辑器预览：取反偏移，圆圈指示玩家应在的位置
            Gizmos.DrawWireSphere((Vector2)transform.position - anchorOffset, 0.2f);
        }
    }
}
