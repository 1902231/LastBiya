using UnityEngine;

/// <summary>
/// 测试用音频管理器（单例）
/// 提供基础的音乐和音效播放功能
/// 注意：这是临时测试版本，正式版需要更完善的功能
/// </summary>
public class AudioManagerTest : MonoBehaviour
{
    public static AudioManagerTest Instance { get; private set; }
    
    [Header("音频源")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("音量设置")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;
    
    [Header("淡入淡出")]
    [SerializeField] private float musicFadeDuration = 1f;
    
    private AudioClip currentMusic;
    private float targetMusicVolume;
    private bool isFading = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 如果没有手动拖拽 AudioSource，自动创建
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
        
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
        targetMusicVolume = musicVolume;
    }

    void Update()
    {
        // 处理音乐淡入淡出
        if (isFading)
        {
            musicSource.volume = Mathf.MoveTowards(
                musicSource.volume,
                targetMusicVolume,
                (musicVolume / musicFadeDuration) * Time.deltaTime
            );
            
            if (Mathf.Approximately(musicSource.volume, targetMusicVolume))
            {
                isFading = false;
                
                // 如果淡出到 0，停止播放
                if (targetMusicVolume == 0f)
                {
                    musicSource.Stop();
                    musicSource.clip = null;
                    currentMusic = null;
                }
            }
        }
    }

    /// <summary>
    /// 播放音乐（带淡入效果）
    /// </summary>
    public void PlayMusic(AudioClip clip, bool fadeIn = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManagerTest: 音乐片段为空！");
            return;
        }
        
        // 如果正在播放相同的音乐，不重复播放
        if (currentMusic == clip && musicSource.isPlaying)
        {
            Debug.Log($"AudioManagerTest: 音乐 '{clip.name}' 已在播放中");
            return;
        }
        
        // 停止当前音乐
        if (musicSource.isPlaying)
        {
            StopMusic(false);
        }
        
        currentMusic = clip;
        musicSource.clip = clip;
        musicSource.Play();
        
        if (fadeIn)
        {
            // 淡入
            musicSource.volume = 0f;
            targetMusicVolume = musicVolume;
            isFading = true;
        }
        else
        {
            musicSource.volume = musicVolume;
        }
        
        Debug.Log($"AudioManagerTest: 播放音乐 '{clip.name}'");
    }

    /// <summary>
    /// 停止音乐（带淡出效果）
    /// </summary>
    public void StopMusic(bool fadeOut = true)
    {
        if (!musicSource.isPlaying)
            return;
        
        if (fadeOut)
        {
            // 淡出
            targetMusicVolume = 0f;
            isFading = true;
        }
        else
        {
            musicSource.Stop();
            musicSource.clip = null;
            currentMusic = null;
        }
        
        Debug.Log("AudioManagerTest: 停止音乐");
    }

    /// <summary>
    /// 暂停音乐
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
            Debug.Log("AudioManagerTest: 暂停音乐");
        }
    }

    /// <summary>
    /// 恢复音乐
    /// </summary>
    public void ResumeMusic()
    {
        if (!musicSource.isPlaying && musicSource.clip != null)
        {
            musicSource.UnPause();
            Debug.Log("AudioManagerTest: 恢复音乐");
        }
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManagerTest: 音效片段为空！");
            return;
        }
        
        sfxSource.PlayOneShot(clip, volumeScale * sfxVolume);
        Debug.Log($"AudioManagerTest: 播放音效 '{clip.name}'");
    }

    /// <summary>
    /// 设置音乐音量
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (!isFading)
        {
            musicSource.volume = musicVolume;
        }
        targetMusicVolume = musicVolume;
    }

    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
    }

    /// <summary>
    /// 静音/取消静音
    /// </summary>
    public void SetMute(bool mute)
    {
        musicSource.mute = mute;
        sfxSource.mute = mute;
        Debug.Log($"AudioManagerTest: {(mute ? "静音" : "取消静音")}");
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
