using UnityEngine;

/// <summary>
/// 场景加载数据结构，用于在 EventCenter 中传递场景加载信息
/// </summary>
public struct SceneLoadData
{
    //方式1 直接加载
    /// <summary>
    /// 要加载的场景 SO
    /// </summary>
    public GameSceneSO sceneToLoad;

    /// <summary>
    /// 玩家传送到的目标位置
    /// </summary>
    public Vector3 targetPosition;

    //方式2 门传送
    public string fromRoomID;
    public string fromDoorID;

    // 新增：目标门信息（用于场景加载后查找门位置）
    public string toRoomID;
    public string toDoorID;

    //加载模式
    public SceneLoadMode loadMode;

    /// <summary>
    /// 是否需要淡入淡出效果
    /// </summary>
    public bool fadeScreen;

    public SceneLoadData(GameSceneSO sceneToLoad, Vector3 targetPosition, bool fadeScreen)
    {
        // 直接加载字段
        this.sceneToLoad = sceneToLoad;
        this.targetPosition = targetPosition;
        this.fadeScreen = fadeScreen;

        // 门传送字段设为默认值
        this.fromRoomID = null;
        this.fromDoorID = null;
        this.toRoomID = null;
        this.toDoorID = null;

        // 标记为直接加载模式
        this.loadMode = SceneLoadMode.Direct;
    }

    /// <summary>
    /// 门传送模式构造函数
    /// </summary>
    public SceneLoadData(string fromRoomID, string fromDoorID, bool fadeScreen = true)
    {
        // 门传送字段
        this.fromRoomID = fromRoomID;
        this.fromDoorID = fromDoorID;
        this.fadeScreen = fadeScreen;

        // 直接加载字段设为默认值
        this.sceneToLoad = null;
        this.targetPosition = Vector3.zero;

        // 目标门信息初始化为null（在SceneLoader中填充）
        this.toRoomID = null;
        this.toDoorID = null;

        // 标记为门传送模式
        this.loadMode = SceneLoadMode.DoorTransition;
    }
}

/// <summary>
/// 场景加载模式枚举
/// </summary>
public enum SceneLoadMode
{
    /// <summary>
    /// 直接加载：指定场景和位置
    /// </summary>
    Direct,

    /// <summary>
    /// 门传送：通过门ID查询连接表
    /// </summary>
    DoorTransition
}
