using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;


public class SceneLoader : MonoBehaviour
{
    // ========== 单例模式 ==========
    private static SceneLoader instance;
    public static SceneLoader Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SceneLoader>();
                if (instance == null)
                {
                    Debug.LogError("场景中没有SceneLoader！请确保SceneLoader存在于场景中。");
                }
            }
            return instance;
        }
    }

    public Transform playerTransform;

    public Vector3 firstPosition;

    [Header("初始场景")]
    public GameSceneSO firstLoadScene;

    [Header("淡入淡出设置")]
    public float fadeDuration;

    private GameSceneSO currentLoadedScene;

    private GameSceneSO sceneToLoad;

    private Vector3 positionToGo;

    private bool fadeScreen;

    private bool isLoading;

    // 新增：保存目标门信息，用于场景加载后查找门位置
    private string targetDoorID;
    private DoorDirection targetDoorDirection;

    [Header("门系统配置")]
    [Tooltip("门连接配置表")]
    public DoorConnectionsTableSO connectionTable;

    [Tooltip("门生成位置偏移量（根据门方向自动计算）")]
    public float doorSpawnOffset = 8f;

    [Header("房间配置")]
    [Tooltip("所有房间的配置列表（用于根据roomID查找GameSceneSO）")]
    public List<GameSceneSO> allRooms = new List<GameSceneSO>();

    private void Awake()
    {
        // 单例模式：确保只有一个实例
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        //Addressables.LoadSceneAsync(firstLoadScene.sceneReference, LoadSceneMode.Additive);
        //currentLoadedScene = firstLoadScene;
        //currentLoadedScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive);
    }


    private void Start()
    {
        NewGame();
    }
    private void OnEnable()
    {
        // 订阅场景加载请求事件
        EventCenter.Instance.AddEventListener<SceneLoadData>("SceneLoadRequest", OnLoadRequestEvent);

    }

    private void OnDisable()
    {
        // 取消订阅场景加载请求事件
        EventCenter.Instance.RemoveEventListener<SceneLoadData>("SceneLoadRequest", OnLoadRequestEvent);

    }

    //初始化场景方法
    private void NewGame()
    {
        sceneToLoad = firstLoadScene;
        OnLoadRequestEvent(new SceneLoadData(sceneToLoad,firstPosition,true));
    }

    //场景加载函数
    private void OnLoadRequestEvent(SceneLoadData data)
    {
        if (isLoading) return;
        isLoading = true;
        this.fadeScreen = data.fadeScreen;

        // 根据加载模式分发处理
        switch (data.loadMode)
        {
            case SceneLoadMode.Direct:
                HandleDirectLoad(data);
                break;

            case SceneLoadMode.DoorTransition:
                HandleDoorTransition(data);
                break;

            default:
                Debug.LogError($"未知的加载模式：{data.loadMode}");
                isLoading = false;
                return;
        }

        // 后续统一加载流程
        if (currentLoadedScene != null)
            StartCoroutine(UnLoadPrevioussScene());
        else
            LoadNewScene();
    }

    //场景卸载函数
    private IEnumerator UnLoadPrevioussScene()
    {
        if (fadeScreen)
        {
            EventCenter.Instance.EventTrigger<float>("FadeIn",fadeDuration);
        }

        yield return new WaitForSeconds(fadeDuration);

        if (currentLoadedScene!= null) 
        {
            yield return currentLoadedScene.sceneReference.UnLoadScene();
        }

       //playerTransform.gameObject.SetActive(false);

        LoadNewScene();
    }

    private void HandleDirectLoad(SceneLoadData data)
    {
        sceneToLoad = data.sceneToLoad;
        positionToGo = data.targetPosition;

        Debug.Log($"直接加载场景：{sceneToLoad.name}，位置：{positionToGo}");
    }

    /// <summary>
    /// 处理门传送模式
    /// </summary>
    private void HandleDoorTransition(SceneLoadData data)
    {
        if (connectionTable == null)
        {
            Debug.LogError("DoorConnectionTable 未配置！");
            isLoading = false;
            return;
        }

        // 1. 查询连接表
        var connection = connectionTable.GetConnection(data.fromRoomID, data.fromDoorID);
        if (connection == null)
        {
            Debug.LogError($"未找到门连接：{data.fromRoomID}.{data.fromDoorID}");
            isLoading = false;
            return;
        }

        // 2. 获取目标房间
        GameSceneSO targetRoom = GetRoomByID(connection.Value.toRoomID);
        if (targetRoom == null)
        {
            Debug.LogError($"未找到目标房间：{connection.Value.toRoomID}");
            isLoading = false;
            return;
        }

        // 3. 获取目标门
        DoorData? targetDoor = targetRoom.GetDoor(connection.Value.toDoorID);
        if (targetDoor == null)
        {
            Debug.LogError($"目标房间 {targetRoom.roomID} 中未找到门 {connection.Value.toDoorID}");
            isLoading = false;
            return;
        }

        // 4. 保存目标门信息（用于场景加载后查找）
        targetDoorID = connection.Value.toDoorID;
        targetDoorDirection = targetDoor.Value.direction;

        // 5. 设置要加载的场景
        sceneToLoad = targetRoom;

        // 6. 先使用默认偏移计算临时位置（场景加载后会重新计算）
        positionToGo = CalculateSpawnPosition(targetDoor.Value.direction);

        Debug.Log($"门传送：{data.fromRoomID}.{data.fromDoorID} → {targetRoom.roomID}.{connection.Value.toDoorID}，临时位置：{positionToGo}");
    }

    //异步加载新场景具体做法
    private void LoadNewScene()
    { 
        var loadingOprition = sceneToLoad.sceneReference.LoadSceneAsync(LoadSceneMode.Additive,true);
        loadingOprition.Completed += OnLoadCompleted;
    }

    //场景加载结束后执行方法
    private void OnLoadCompleted(AsyncOperationHandle<SceneInstance> handle)
    {
        currentLoadedScene = sceneToLoad;

        // 尝试从新加载的场景中查找目标门的精确位置
        Vector3 finalPosition = CalculateSpawnPositionFromScene(targetDoorID, targetDoorDirection);

        // 如果没找到门，使用默认偏移位置
        if (finalPosition == Vector3.zero)
        {
            finalPosition = positionToGo;
            Debug.LogWarning($"未在场景中找到门 {targetDoorID}，使用默认偏移位置：{finalPosition}");
        }
        else
        {
            Debug.Log($"找到目标门位置，玩家出生在：{finalPosition}");
        }

        playerTransform.position = finalPosition;
        //playerTransform.gameObject.SetActive(true);

        if (fadeScreen)
        {
            //TODO：实现淡出
            EventCenter.Instance.EventTrigger<float>("FadeOut", fadeDuration);
        }

        isLoading = false;

        // 触发场景加载完成事件
        EventCenter.Instance.EventTrigger<GameSceneSO>("SceneLoadCompleted", currentLoadedScene);
    }

    //新方法

    /// <summary>
    /// 获取当前加载的房间（供Door组件调用）
    /// </summary>
    public GameSceneSO GetCurrentRoom()
    {
        return currentLoadedScene;
    }

    /// <summary>
    /// 根据房间ID查找GameSceneSO
    /// </summary>
    private GameSceneSO GetRoomByID(string roomID)
    {
        foreach (var room in allRooms)
        {
            if (room.roomID == roomID)
                return room;
        }
        Debug.LogError($"未找到房间：{roomID}");
        return null;
    }

   
    /// <summary>
    /// 根据门方向计算玩家生成位置
    /// </summary>
    private Vector3 CalculateSpawnPosition(DoorDirection direction)
    {
        switch (direction)
        {
            case DoorDirection.Left:
                return new Vector3(-doorSpawnOffset, 0, 0);
            case DoorDirection.Right:
                return new Vector3(doorSpawnOffset, 0, 0);
            case DoorDirection.Top:
                return new Vector3(0, doorSpawnOffset, 0);
            case DoorDirection.Bottom:
                return new Vector3(0, -doorSpawnOffset, 0);
            default:
                return Vector3.zero;
        }
    }

    /// <summary>
    /// 从已加载的场景中查找门的实际位置并计算出生点
    /// </summary>
    private Vector3 CalculateSpawnPositionFromScene(string doorID, DoorDirection direction)
    {
        // 如果门ID为空，返回零向量
        if (string.IsNullOrEmpty(doorID))
            return Vector3.zero;

        // 查找目标场景中的所有Door组件（包括未激活的对象）
        Door[] allDoors = FindObjectsOfType<Door>(true);

        foreach (Door door in allDoors)
        {
            if (door.doorID == doorID)
            {
                // 找到目标门，根据门的位置和方向计算玩家出生位置
                Vector3 doorPosition = door.GetDoorPosition();
                Vector3 offset = GetOffsetByDirection(direction);

                return doorPosition + offset;
            }
        }

        // 未找到门，返回零向量
        return Vector3.zero;
    }

    /// <summary>
    /// 根据方向获取偏移向量
    /// </summary>
    private Vector3 GetOffsetByDirection(DoorDirection direction)
    {
        switch (direction)
        {
            case DoorDirection.Left:
                return new Vector3(-doorSpawnOffset, 0, 0);
            case DoorDirection.Right:
                return new Vector3(doorSpawnOffset, 0, 0);
            case DoorDirection.Top:
                return new Vector3(0, doorSpawnOffset, 0);
            case DoorDirection.Bottom:
                return new Vector3(0, -doorSpawnOffset, 0);
            default:
                return Vector3.zero;
        }
    }
}