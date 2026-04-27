using UnityEngine;

/// <summary>
/// 追踪符文
/// 阶段：从书本位置飞到上空随机悬停点 → 停顿 → 追踪目标
/// 到达目标后根据目标是否死亡（DeadUnit Layer）决定攻击或消失
/// </summary>
public class TrackingRune : MonoBehaviour
{
    private Transform target;
    private float damage;
    private float postureDamage;
    private float hoverSpeed;
    private float currentTrackSpeed;
    private float trackAcceleration;
    private float pauseDuration;
    private Vector2 hoverPosition;
    private GameObject hitEffect;
    private GameObject vanishEffect;

    private Phase currentPhase;
    private float timer;
    private float arriveDistance = 0.3f;

    private enum Phase
    {
        FlyToHover,     // 从书本飞到悬停点
        Pause,          // 停顿
        Tracking        // 追踪目标
    }

    public void Initialize(Transform target, float damage, float postureDamage, float hoverSpeed, float trackSpeed,
        float trackAcceleration, float pauseDuration, Vector2 hoverPos,
        GameObject hitEffect, GameObject vanishEffect)
    {
        this.target = target;
        this.damage = damage;
        this.postureDamage = postureDamage;
        this.hoverSpeed = hoverSpeed;
        this.currentTrackSpeed = trackSpeed;
        this.trackAcceleration = trackAcceleration;
        this.pauseDuration = pauseDuration;
        this.hoverPosition = hoverPos;
        this.hitEffect = hitEffect;
        this.vanishEffect = vanishEffect;

        currentPhase = Phase.FlyToHover;
        timer = 0f;
    }

    void Update()
    {
        switch (currentPhase)
        {
            case Phase.FlyToHover:
                transform.position = Vector2.MoveTowards(
                    transform.position, hoverPosition, hoverSpeed * Time.deltaTime);

                if (Vector2.Distance(transform.position, hoverPosition) <= 0.05f)
                {
                    currentPhase = Phase.Pause;
                    timer = 0f;
                }
                break;

            case Phase.Pause:
                timer += Time.deltaTime;
                if (timer >= pauseDuration)
                {
                    currentPhase = Phase.Tracking;
                }
                break;

            case Phase.Tracking:
                TrackTarget();
                break;
        }
    }

    private void TrackTarget()
    {
        if (target == null)
        {
            SpawnVanishEffect();
            Destroy(gameObject);
            return;
        }

        // 加速
        currentTrackSpeed += trackAcceleration * Time.deltaTime;

        transform.position = Vector2.MoveTowards(
            transform.position, target.position, currentTrackSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.position) <= arriveDistance)
        {
            OnArrived();
        }
    }

    private void OnArrived()
    {
        if (target.gameObject.layer == LayerMask.NameToLayer("DeadUnit"))
        {
            SpawnVanishEffect();
        }
        else
        {
            OnHitTarget();
        }

        Destroy(gameObject);
    }

    private void OnHitTarget()
    {
        var damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            Vector2 knockbackDir = (target.position - transform.position).normalized;
            var info = new DamageInfo((int)damage, knockbackDir, 0f, DamageSource.Player, postureDamage);
            damageable.TakeDamage(info);
        }

        if (hitEffect != null)
            Instantiate(hitEffect, transform.position, Quaternion.identity);
    }

    private void SpawnVanishEffect()
    {
        if (vanishEffect != null)
            Instantiate(vanishEffect, transform.position, Quaternion.identity);
    }
}
