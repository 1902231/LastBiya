using System.Collections;
using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
using UnityEngine;

/// <summary>
/// 门连接配置表so
/// </summary>
[CreateAssetMenu(menuName = "Game Scene/Door Connection Table")]
public class DoorConnectionsTableSO : ScriptableObject
{
    [Header("所有门连接")]
    public List<DoorConnection> connections = new List<DoorConnection>();

    /// <summary>
    /// 根据来源门查找目标门
    /// </summary>
    /// <param name="fromRoomID">来源房间ID</param>
    /// <param name="fromDoorID">来源门ID</param>
    /// <returns>目标门连接配置，未找到返回null</returns>
    public DoorConnection? GetConnection(string fromRoomID, string fromDoorID)
    {

        foreach (var connection in connections)
        {
            if (connection.fromRoomID == fromRoomID && connection.fromDoorID == fromDoorID)
            {
                return connection;
            }
        }
        Debug.LogWarning($"未找到门连接：{fromRoomID}.{fromDoorID}");
        return null;
    }

    /// <summary>
    /// 验证连接表完整性方法
    /// </summary>
    public void ValidateConnections()
    {
        int errorCount = 0;
        foreach (var connection in connections)
        {
            var reverse = GetConnection(connection.toRoomID, connection.toDoorID);
            if (reverse == null)
            {
                Debug.LogError($"缺少反向连接：{connection.toRoomID}.{connection.toDoorID} ==> {connection.fromRoomID}.{connection.fromDoorID}");
                errorCount++;
            }
            else if (reverse.Value.toRoomID != connection.fromRoomID || reverse.Value.toDoorID != connection.fromDoorID)
            {
                Debug.LogError($"反向连接不匹配：{connection.fromRoomID}.{connection.fromDoorID} <==> {connection.toRoomID}.{connection.toDoorID}");
                errorCount++;
            }
        }
        if (errorCount == 0)
        {
            Debug.Log($"连接表验证通过！共 {connections.Count} 个连接");
        }
        else
        {
            Debug.LogError($"连接表验证失败！发现 {errorCount} 个错误");
        }
    }
}

/// <summary>
/// 单个门连接数据结构体
/// </summary>
[System.Serializable]
public struct DoorConnection
{
    [Tooltip("来源房间ID")]
    public string fromRoomID;

    [Tooltip("来源门ID")]
    public string fromDoorID;

    [Tooltip("目标房间ID")]
    public string toRoomID;

    [Tooltip("目标门ID")]
    public string toDoorID;

    public DoorConnection(string fromRoomID, string fromDoorID, string toRoomID, string toDoorID)
    {
        this.fromRoomID = fromRoomID;
        this.fromDoorID = fromDoorID;
        this.toRoomID = toRoomID;
        this.toDoorID = toDoorID;
    }

    public override string ToString()
    {
        return $"{fromRoomID}.{fromDoorID} ==> {toRoomID}.{toDoorID}";
    }
}
