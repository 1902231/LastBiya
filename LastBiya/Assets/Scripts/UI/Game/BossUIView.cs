using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boss UI View：血条 + 架势条（各带缓冲）+ Boss 名称
/// 可复用于不同 Boss
/// </summary>
public class BossUIView : MonoBehaviour
{
    [Header("血条")]
    public Image bossHp;
    public Image bossHpBuffer;

    [Header("架势条")]
    public Image bossPosture;
    public Image bossPostureBuffer;

    [Header("名称")]
    public Text bossNameText;

    [Header("缓冲设置")]
    [Tooltip("缓冲条延迟追赶时间")]
    public float bufferDelay = 0.2f;
    [Tooltip("缓冲条追赶速度")]
    public float bufferChaseSpeed = 3f;

    // 血条缓冲
    private float hpDelayTimer;
    private bool isHpChasing;

    // 架势条缓冲
    private float postureDelayTimer;
    private bool isPostureChasing;

    public void SetBossName(string name)
    {
        if (bossNameText != null)
            bossNameText.text = name;
    }

    public void UpdateHP(float hpPercent)
    {
        hpPercent = Mathf.Clamp01(hpPercent);
        bossHp.fillAmount = hpPercent;
        hpDelayTimer = bufferDelay;
        isHpChasing = false;
    }

    public void UpdatePosture(float posturePercent)
    {
        posturePercent = Mathf.Clamp01(posturePercent);
        bossPosture.fillAmount = posturePercent;
        postureDelayTimer = bufferDelay;
        isPostureChasing = false;
    }

    /// <summary>
    /// 初始化所有条到满值
    /// </summary>
    public void InitFull()
    {
        bossHp.fillAmount = 1f;
        bossHpBuffer.fillAmount = 1f;
        bossPosture.fillAmount = 1f;
        bossPostureBuffer.fillAmount = 1f;
    }

    void Update()
    {
        TickHpBuffer();
        ChaseHpBuffer();
        TickPostureBuffer();
        ChasePostureBuffer();
    }

    private void TickHpBuffer()
    {
        if (hpDelayTimer <= 0) return;
        hpDelayTimer -= Time.deltaTime;
        if (hpDelayTimer <= 0)
            isHpChasing = true;
    }

    private void ChaseHpBuffer()
    {
        if (!isHpChasing) return;
        bossHpBuffer.fillAmount = Mathf.Lerp(
            bossHpBuffer.fillAmount, bossHp.fillAmount, Time.deltaTime * bufferChaseSpeed);
        if (Mathf.Abs(bossHpBuffer.fillAmount - bossHp.fillAmount) <= 0.001f)
        {
            bossHpBuffer.fillAmount = bossHp.fillAmount;
            isHpChasing = false;
        }
    }

    private void TickPostureBuffer()
    {
        if (postureDelayTimer <= 0) return;
        postureDelayTimer -= Time.deltaTime;
        if (postureDelayTimer <= 0)
            isPostureChasing = true;
    }

    private void ChasePostureBuffer()
    {
        if (!isPostureChasing) return;
        bossPostureBuffer.fillAmount = Mathf.Lerp(
            bossPostureBuffer.fillAmount, bossPosture.fillAmount, Time.deltaTime * bufferChaseSpeed);
        if (Mathf.Abs(bossPostureBuffer.fillAmount - bossPosture.fillAmount) <= 0.001f)
        {
            bossPostureBuffer.fillAmount = bossPosture.fillAmount;
            isPostureChasing = false;
        }
    }
}
