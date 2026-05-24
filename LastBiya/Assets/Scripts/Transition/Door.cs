using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 门组件 - 挂载在场景中的门物体上
/// 玩家接触后触发场景切换
/// 需要配合 Collider2D 组件使用，并勾选 Is Trigger
/// </summary>
public class Door : MonoBehaviour
{
    //[Header("目标场景")]
    //[Tooltip("要传送到的目标场景")]
    //public GameSceneSO sceneToGo;

    //[Tooltip("玩家在目标场景中的生成位置")]
    //public Vector3 positionToGo;

    [Header("门标识")]
    [Tooltip("当前门的id，在单个房间中是唯一的")]
    public string doorID = "Left_01";

    [Header("门方向")]
    [Tooltip("门的朝向，用于计算玩家出生位置")]
    public DoorDirection doorDirection = DoorDirection.Right;

    [Header("触发设置")]
    [Tooltip("是否启用门")]
    public bool isActive = true;

    [Tooltip("防止重复触发（勾选后只能触发一次）")]
    public bool triggerOnce = false;

    private bool hasTriggered = false;
    private GameSceneSO currentRoom;
    private SceneLoader sceneLoader;

    private void Start()
    {
        // 使用单例模式获取SceneLoader引用
        sceneLoader = SceneLoader.Instance;
        
        if (sceneLoader == null)
        {
            Debug.LogError($"门 {doorID} 无法找到SceneLoader！");
        }
    }

    /// <summary>
    /// 获取当前房间信息（延迟获取，确保场景已加载）
    /// </summary>
    private GameSceneSO GetCurrentRoom()
    {
        if (currentRoom == null && sceneLoader != null)
        {
            currentRoom = sceneLoader.GetCurrentRoom();
        }
        return currentRoom;
    }

    /// <summary>
    /// 玩家进入触发器时自动触发传送
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 检查是否是玩家
        if (!collision.CompareTag("Player"))
            return;

        // 检查传送点是否激活
        if (!isActive)
        {
            Debug.Log("传送点未激活");
            return;
        }

        // 检查是否已经触发过
        if (triggerOnce && hasTriggered)
        {
            Debug.Log("传送点已使用过");
            return;
        }

        // 触发传送
        //TriggerAction();
        TriggerTransition();
        // 标记已触发
        if (triggerOnce)
            hasTriggered = true;
    }



    /// <summary>
    /// 触发场景切换
    /// </summary>
    public void TriggerTransition()
    {
        // 获取当前房间信息（延迟获取）
        GameSceneSO room = GetCurrentRoom();
        
        if (room == null)
        {
            Debug.LogError("当前房间信息为空，无法触发传送！");
            return;
        }

        Debug.Log($"玩家从门 {room.roomID}.{doorID} 进入");

        // 使用新的构造函数（门传送模式）
        var loadData = new SceneLoadData(room.roomID, doorID, true);

        // 触发统一的场景加载事件
        EventCenter.Instance.EventTrigger<SceneLoadData>("SceneLoadRequest", loadData);
    }

    /// <summary>
    /// 获取这个门的世界坐标位置
    /// </summary>
    public Vector3 GetDoorPosition()
    {
        return transform.position;
    }

    #region 原传送逻辑
    /// <summary>
    /// 触发场景切换
    /// </summary>
    //public void TriggerAction()
    //{
    //    Debug.Log($"传送到场景：{sceneToGo.name}，位置：{positionToGo}");

    //    // 创建场景加载数据
    //    var loadData = new SceneLoadData(sceneToGo, positionToGo, true);

    //    // 通过 EventCenter 触发场景加载请求
    //    EventCenter.Instance.EventTrigger<SceneLoadData>("SceneLoadRequest", loadData);
    //}

    ///// <summary>
    ///// 在编辑器中绘制传送点位置（方便调试）
    ///// </summary>
    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = isActive ? Color.cyan : Color.gray;
    //    Gizmos.DrawWireSphere(transform.position, 0.5f);

    //    // 绘制箭头指示传送方向
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);
    //}

    //private void OnDrawGizmosSelected()
    //{
    //    // 选中时显示更详细的信息
    //    Gizmos.color = Color.cyan;
    //    Gizmos.DrawWireSphere(transform.position, 1f);

    //    #if UNITY_EDITOR
    //    UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, 
    //        $"传送点\n目标: {(sceneToGo != null ? sceneToGo.name : "未设置")}\n位置: {positionToGo}");
    //    #endif
    //}
    #endregion
}

