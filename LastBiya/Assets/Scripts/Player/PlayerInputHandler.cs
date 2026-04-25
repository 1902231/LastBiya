using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//玩家输入处理器脚本，截取玩家输入，保存对应事件
//暂时弃用
public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInput playerInput;

    // 缓存的 Action 引用
    public InputAction MoveAction { get; private set; }
    public InputAction JumpAction { get; private set; }
    public InputAction AttackAction { get; private set; }
    public InputAction DashAction { get; private set; }

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        // 通过名字找到 Action，只找一次，缓存起来
        MoveAction = playerInput.actions["Move"];
        JumpAction = playerInput.actions["Jump"];
        AttackAction = playerInput.actions["Attack"];
        DashAction = playerInput.actions["Dash"];
    }
}
