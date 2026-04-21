using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 全局输入管理器（单例）
/// 通过直接引用 InputActionAsset 管理所有输入，不依赖任何 GameObject 上的 PlayerInput 组件
/// 各系统通过 InputManager.Instance.XxxAction 访问输入
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("拖入 PlayerInputActions 资产文件")]
    [SerializeField] private InputActionAsset inputActions;

    //Action Map 引用
    private InputActionMap playerNormalMap;
    // 之后扩展：
    // private InputActionMap uiMap;
    // private InputActionMap dialogueMap;

    public InputAction MoveAction { get; private set; }
    public InputAction JumpAction { get; private set; }
    public InputAction AttackAction { get; private set; }
    public InputAction DashAction { get; private set; }


    void Awake()
    {
        // 单例保护
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 从资产文件中查找 Action Map
        playerNormalMap = inputActions.FindActionMap("PlayerNormal");

        // 从 Map 中查找具体 Action
        MoveAction   = playerNormalMap.FindAction("Move");
        JumpAction   = playerNormalMap.FindAction("Jump");
        AttackAction = playerNormalMap.FindAction("Attack");
        DashAction   = playerNormalMap.FindAction("Dash");

        // 默认启用 PlayerNormal
        playerNormalMap.Enable();
    }

    void OnDestroy()
    {
        playerNormalMap?.Disable();
        // uiMap?.Disable();
        // dialogueMap?.Disable();

        // 如果是当前单例被销毁，清空引用
        if (Instance == this)
            Instance = null;
    }

    // public void SwitchToPlayerNormal()
    // {
    //     uiMap?.Disable();
    //     dialogueMap?.Disable();
    //     playerNormalMap?.Enable();
    // }

    // public void SwitchToUI()
    // {
    //     playerNormalMap?.Disable();
    //     dialogueMap?.Disable();
    //     uiMap?.Enable();
    // }

    // public void SwitchToDialogue()
    // {
    //     playerNormalMap?.Disable();
    //     uiMap?.Disable();
    //     dialogueMap?.Enable();
    // }
}
