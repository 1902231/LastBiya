using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName =("Game Scene/GameSceneSO"),order =0)]
public class GameSceneSO :ScriptableObject
{

    [Header("房间标识")]
    [Tooltip("房间唯一ID，用于门连接系统")]
    public string roomID;

    [Header("场景引用")]
    public AssetReference sceneReference;
    public ESceneType sceneType;

    [Header("门配置")]
    [Tooltip("该房间的所有门")]
    public List<DoorData> doors;

    //查找当前房间所有的门
    public DoorData? GetDoor(string doorID)
    {
        // 空检查：如果doors列表为null或为空，直接返回
        if (doors == null || doors.Count == 0)
        {
            Debug.LogError($"房间 {roomID} 的doors列表为空！请在Inspector中配置门列表。");
            return null;
        }

        foreach (var door in doors)
        {
            if (door.doorID == doorID)
                return door;
        }
        Debug.LogWarning($"房间 {roomID} 中未找到门 {doorID}");
        return null;
    }
}
public enum ESceneType
{
    Menu,
    Location,
}

