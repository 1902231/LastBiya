using UnityEngine;

/// <summary>
/// 追踪符文攻击能力（瞬发型）
/// 在书本位置生成符文，符文飞到上空随机点后再追踪目标
/// </summary>
public class BookAbility_RuneAttack : BaseAbility<BookCotroller>
{
    private Transform currentTarget;

    public BookAbility_RuneAttack()
    {
        priority = 0;
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    public override void Activate()
    {
        base.Activate();

        if (owner.runePrefab != null && currentTarget != null)
        {
            // 在书本位置生成符文
            Vector2 bookPos = owner.transform.position;
            GameObject rune = Object.Instantiate(owner.runePrefab, bookPos, Quaternion.identity);

            // 计算悬停点（书本上空随机位置）
            Vector2 hoverPos = bookPos + new Vector2(
                Random.Range(-owner.runeSpawnRange, owner.runeSpawnRange),
                Random.Range(0.5f, owner.runeSpawnRange)
            );

            var runeScript = rune.GetComponent<TrackingRune>();
            if (runeScript != null)
            {
                runeScript.Initialize(
                    currentTarget,
                    owner.runeDamage,
                    owner.runePostureDamage,
                    owner.runeHoverSpeed,
                    owner.runeTrackSpeed,
                    owner.runeTrackAcceleration,
                    owner.runePauseDuration,
                    hoverPos,
                    owner.runeHitEffect,
                    owner.runeVanishEffect
                );
            }
        }

        currentTarget = null;
        isActive = false;
    }
}
