using UnityEngine;

/// <summary>
/// Boss 战触发器
/// 持有 Boss 战的所有配置信息
/// </summary>
public class BossBattleTrigger : MonoBehaviour
{
    [Header("Boss 配置")]
    [Tooltip("拖拽 Boss 物体（必须实现 IBoss 接口）")]
    [SerializeField] private MonoBehaviour bossObject;
    
    [Header("房间门配置")]
    [Tooltip("拖拽所有房间门物体")]
    [SerializeField] private GameObject[] roomDoors;
    
    [Header("相机配置")]
    [Tooltip("相机聚焦点（可选，留空则聚焦 Boss）")]
    [SerializeField] private Transform cameraFocusPoint;
    
    [Tooltip("相机缩放大小")]
    [SerializeField] private float cameraZoomSize = 8f;
    
    [Tooltip("相机缩放过渡时间")]
    [SerializeField] private float cameraZoomDuration = 1.5f;
    
    [Header("音乐配置")]
    [Tooltip("Boss 战音乐（可选）")]
    [SerializeField] private AudioClip battleMusic;
    
    [Tooltip("开场音效（可选）")]
    [SerializeField] private AudioClip introSFX;
    
    [Header("UI 配置")]
    [Tooltip("Boss 名称显示时长（秒）")]
    [SerializeField] private float bossNameDisplayDuration = 2f;
    
    [Header("触发设置")]
    [SerializeField] private bool triggerOnce = true;
    
    private bool hasTriggered = false;
    private IBoss boss;

    // ===== 公共属性（供 BossBattleManager 访问）=====
    public IBoss Boss => boss;
    public GameObject[] RoomDoors => roomDoors;
    public Transform CameraFocusPoint => cameraFocusPoint != null ? cameraFocusPoint : boss?.Transform;
    public float CameraZoomSize => cameraZoomSize;
    public float CameraZoomDuration => cameraZoomDuration;
    public AudioClip BattleMusic => battleMusic;
    public AudioClip IntroSFX => introSFX;
    public float BossNameDisplayDuration => bossNameDisplayDuration;

    void Start()
    {
        // 验证 Boss 引用
        if (bossObject != null)
        {
            boss = bossObject as IBoss;
            if (boss == null)
            {
                Debug.LogError($"Boss 物体 '{bossObject.name}' 没有实现 IBoss 接口！");
            }
        }
        else
        {
            Debug.LogError("BossBattleTrigger: Boss 物体未设置！");
        }
        
        // 预加载音频资源，避免触发时卡顿
        PreloadAudio();
    }
    
    /// <summary>
    /// 预加载音频资源
    /// </summary>
    private void PreloadAudio()
    {
        if (battleMusic != null)
        {
            battleMusic.LoadAudioData();
            Debug.Log($"[BossBattleTrigger] 预加载音乐: {battleMusic.name}");
        }
        
        if (introSFX != null)
        {
            introSFX.LoadAudioData();
            Debug.Log($"[BossBattleTrigger] 预加载音效: {introSFX.name}");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered && triggerOnce) return;
        if (boss == null) return;
        
        if (other.CompareTag("Player"))
        {
            TriggerBossBattle();
        }
    }

    private void TriggerBossBattle()
    {
        // 触发事件，传递触发器自己的引用
        EventCenter.Instance.EventTrigger<BossBattleTrigger>("StartBossBattle", this);
        
        hasTriggered = true;
        GetComponent<Collider2D>().enabled = false;
    }
}
