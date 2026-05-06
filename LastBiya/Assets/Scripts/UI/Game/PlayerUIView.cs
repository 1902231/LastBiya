using UnityEngine;
using UnityEngine.UI;

public class PlayerUIView : MonoBehaviour
{
    [Header("元素瓶")]
    public Image HealthPotion;
    public Image HealthPosionBackGround;

    [Header("血条")]
    public GameObject playerHpBar;
    public Image playerHp;
    public Image playerHpBuffer;
    [Tooltip("缓冲条延迟追赶时间")]
    public float bufferDelay = 0.2f;
    [Tooltip("缓冲条追赶速度")]
    public float bufferChaseSpeed = 3f;

    private float delayTimer;
    private bool isChasing;

    public void UpdateHP(float hpPercent)
    {
        hpPercent = Mathf.Clamp01(hpPercent);
        playerHp.fillAmount = hpPercent;

        // 每次血量变化，重置延迟
        delayTimer = bufferDelay;
        isChasing = false;
    }

    void Update()
    {
        TickBufferDelay();
        ChaseBuffer();
    }

    private void TickBufferDelay()
    {
        if (delayTimer <= 0) return;

        delayTimer -= Time.deltaTime;
        if (delayTimer <= 0)
            isChasing = true;
    }

    private void ChaseBuffer()
    {
        if (!isChasing) return;

        playerHpBuffer.fillAmount = Mathf.Lerp(
            playerHpBuffer.fillAmount, playerHp.fillAmount, Time.deltaTime * bufferChaseSpeed);

        if (Mathf.Abs(playerHpBuffer.fillAmount - playerHp.fillAmount) <= 0.001f)
        {
            playerHpBuffer.fillAmount = playerHp.fillAmount;
            isChasing = false;
        }
    }
}
