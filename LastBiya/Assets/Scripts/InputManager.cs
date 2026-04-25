using System.Collections.Generic;
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

    // —— Actions ——
    public InputAction MoveAction { get; private set; }
    public Vector2 MoveInput => MoveAction.ReadValue<Vector2>();
    public InputAction JumpAction { get; private set; }
    public InputAction AttackAction { get; private set; }
    public InputAction DashAction { get; private set; }
    public InputAction FallingDashAction { get; private set; }
    public InputAction ChargeAttackAction { get; private set; }

    // —— 输入缓冲 ——
    [Header("输入缓冲")]
    [SerializeField] private float bufferDuration = 0.15f;

    private Dictionary<InputAction, float> bufferTimers = new();

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
        FallingDashAction = playerNormalMap.FindAction("FallingDash");
        ChargeAttackAction = playerNormalMap.FindAction("ChargeAttack");

        // 注册需要缓冲的 Action（Move 是 Value 类型，不需要缓冲）
        // Dash 不加缓冲，避免下冲落地后自动接普通冲刺
        bufferTimers[JumpAction]   = 0;
        bufferTimers[AttackAction] = 0;
        bufferTimers[FallingDashAction] = 0;

        // 默认启用 PlayerNormal
        playerNormalMap.Enable();
    }

    void Update()
    {
        // 统一遍历：倒计时 + 检测按下
        var keys = new List<InputAction>(bufferTimers.Keys);
        foreach (var action in keys)
        {
            bufferTimers[action] -= Time.deltaTime;
            if (action.WasPressedThisFrame())
                bufferTimers[action] = bufferDuration;
        }
    }

    /// <summary>
    /// 消费指定 Action 的缓冲输入，返回 true 表示缓冲窗口内有该输入，读完自动清零
    /// </summary>
    public bool Consume(InputAction action)
    {
        if (bufferTimers.TryGetValue(action, out float timer) && timer > 0)
        {
            bufferTimers[action] = 0;
            return true;
        }
        return false;
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
