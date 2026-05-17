using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �ŵķ���ö��
/// </summary>
public enum DoorDirection
{
    Left,
    Right,
    Top,
    Bottom
}

/// <summary>
/// 门数据结构体
/// </summary>
[System.Serializable]  // ← 必须添加这个特性，Unity才能在Inspector中显示
public struct DoorData
{
    [Tooltip("门的唯一标识，如 Left_01, Right_01")]
    public string doorID;

    [Tooltip("门的方向")]
    public DoorDirection direction;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DoorData(string doorID, DoorDirection direction)
    {
        this.doorID = doorID;
        this.direction = direction;
    }
}
