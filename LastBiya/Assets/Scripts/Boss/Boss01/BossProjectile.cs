using UnityEngine;

/// <summary>
/// Boss 远程攻击弹射物
/// 阶段：飞出 → 停顿 → 返回 → 到达Boss后销毁
/// </summary>
public class BossProjectile : MonoBehaviour
{
    private int direction;
    private float speed;
    private float maxDistance;
    private float damage;
    private Transform bossTransform;

    private Vector2 startPosition;
    private bool isStopped;
    private bool isReturning;
    private bool hasReturned;
    private float pauseTimer;
    private float pauseDuration;

    private Rigidbody2D rb;
    private AttackHitbox hitbox;

    public bool IsStopped => isStopped;
    public bool IsReturning => isReturning;
    public bool HasReturned => hasReturned;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitbox = GetComponentInChildren<AttackHitbox>(true);
    }

    /// <summary>
    /// 初始化弹射物
    /// </summary>
    public void Initialize(int dir, float spd, float maxDist, float dmg, Transform boss, float pause)
    {
        direction = dir;
        speed = spd;
        maxDistance = maxDist;
        damage = dmg;
        bossTransform = boss;
        pauseDuration = pause;

        startPosition = transform.position;
        isStopped = false;
        isReturning = false;
        hasReturned = false;
        pauseTimer = 0f;

        if (hitbox != null)
            hitbox.damage = (int)damage;
    }

    void Update()
    {
        if (hasReturned) return;

        if (!isStopped && !isReturning)
        {
            // 飞行阶段：检查是否到达最大距离
            float traveled = Vector2.Distance(startPosition, transform.position);
            if (traveled >= maxDistance)
                Stop();
        }
        else if (isStopped && !isReturning)
        {
            // 停顿阶段
            pauseTimer += Time.deltaTime;
            if (pauseTimer >= pauseDuration)
                StartReturning();
        }
        else if (isReturning)
        {
            // 返回阶段：检查是否到达Boss
            if (bossTransform == null)
            {
                Destroy(gameObject);
                return;
            }
            if (Vector2.Distance(transform.position, bossTransform.position) <= 0.5f)
            {
                hasReturned = true;
                Destroy(gameObject);
            }
        }
    }

    void FixedUpdate()
    {
        if (hasReturned || isStopped) 
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (!isReturning)
        {
            rb.velocity = Vector2.right * direction * speed;
        }
        else
        {
            if (bossTransform == null) return;
            Vector2 dirToBoss = ((Vector2)bossTransform.position - (Vector2)transform.position).normalized;
            rb.velocity = dirToBoss * speed;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isStopped && !isReturning)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
                Stop();
        }
    }

    private void Stop()
    {
        if (isStopped) return;
        isStopped = true;
        rb.velocity = Vector2.zero;
    }

    private void StartReturning()
    {
        isReturning = true;
        isStopped = false;

        // 重置命中记录，返回途中可以再次造成伤害
        if (hitbox != null)
            hitbox.ResetHitRecord();
    }
}
