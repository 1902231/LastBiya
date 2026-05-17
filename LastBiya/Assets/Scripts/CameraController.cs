using UnityEngine;
using Cinemachine;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 相机控制器（单例）
/// 管理 Cinemachine 虚拟相机的聚焦、缩放等
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }
    
    [Header("主虚拟相机")]
    [SerializeField] private CinemachineVirtualCamera mainVirtualCamera;
    
    private float originalOrthoSize;
    private Transform originalFollowTarget;
    private Tweener currentZoomTween;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (mainVirtualCamera != null)
        {
            originalOrthoSize = mainVirtualCamera.m_Lens.OrthographicSize;
            originalFollowTarget = mainVirtualCamera.Follow;
        }
        else
        {
            Debug.LogWarning("CameraController: 主虚拟相机未设置！");
        }
    }

    /// <summary>
    /// 聚焦到目标并缩放（使用 DOTween）
    /// </summary>
    public void FocusAndZoom(Transform target, float zoomSize, float duration)
    {
        if (mainVirtualCamera == null)
        {
            Debug.LogWarning("CameraController: 主虚拟相机未设置，无法聚焦！");
            return;
        }
        
        if (target == null)
        {
            Debug.LogWarning("CameraController: 聚焦目标为空！");
            return;
        }
        
        // 取消之前的缩放动画
        currentZoomTween?.Kill();
        
        // 设置跟随目标
        mainVirtualCamera.Follow = target;
        
        // 使用 DOTween 平滑缩放
        currentZoomTween = DOTween.To(
            () => mainVirtualCamera.m_Lens.OrthographicSize,
            x => mainVirtualCamera.m_Lens.OrthographicSize = x,
            zoomSize,
            duration
        ).SetEase(Ease.InOutQuad);
    }

    /// <summary>
    /// 恢复原始相机设置（平滑过渡）
    /// </summary>
    public void ResetCamera(float duration = 1f)
    {
        if (mainVirtualCamera == null) return;
        
        currentZoomTween?.Kill();
        
        // 恢复跟随目标（Cinemachine 会根据 Body 的 Damping 设置自动平滑过渡）
        mainVirtualCamera.Follow = originalFollowTarget;
        
        // 同时恢复缩放
        currentZoomTween = DOTween.To(
            () => mainVirtualCamera.m_Lens.OrthographicSize,
            x => mainVirtualCamera.m_Lens.OrthographicSize = x,
            originalOrthoSize,
            duration
        ).SetEase(Ease.InOutQuad);
    }

    /// <summary>
    /// 相机震动（可选，需要 DOTween）
    /// </summary>
    public void Shake(float duration, float strength)
    {
        if (mainVirtualCamera == null) return;
        
        // 使用 DOTween 实现震动
        mainVirtualCamera.transform.DOShakePosition(duration, strength);
    }

    void OnDestroy()
    {
        // 清理 DOTween 动画
        currentZoomTween?.Kill();
        
        if (Instance == this)
            Instance = null;
    }
}
