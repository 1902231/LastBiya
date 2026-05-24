using UnityEngine;

/// <summary>
/// Boss 通用接口
/// 所有 Boss 都必须实现此接口
/// </summary>
public interface IBoss : IUnit
{
    /// <summary>
    /// Boss 唯一标识（例如 "Boss01", "Boss02"）
    /// </summary>
    string BossID { get; }
    
    /// <summary>
    /// Boss 是否已死亡
    /// </summary>
    bool IsDead { get; }
    
    /// <summary>
    /// Boss 的 Transform（用于相机聚焦）
    /// </summary>
    Transform Transform { get; }
    
    /// <summary>
    /// 启动 Boss 战斗逻辑（启动行为树）
    /// </summary>
    void StartCombat();
    
    /// <summary>
    /// 停止 Boss 战斗逻辑（停止行为树）
    /// </summary>
    void StopCombat();
    
    /// <summary>
    /// 播放开场动画
    /// </summary>
    /// <returns>动画时长（秒）</returns>
    float PlayIntroAnimation();
}
