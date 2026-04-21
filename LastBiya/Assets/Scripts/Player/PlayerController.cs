using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_PlayerStateType
{
    //父状态
    Active,
    Grounded,
    AirBornd,
    //子状态
    Idle,
    Move,

}

public enum E_PlayerAbilityType
{ 
    
}

public class PlayerController : MonoBehaviour
{
    private HFSM<E_PlayerStateType, PlayerController> playerFsm;
    private AbilityManager<E_PlayerAbilityType, PlayerController> PlayerAbilityManager;

    //[SerializeField]private PlayerInputHandler inputHandler;
    //public PlayerInputHandler Input => inputHandler;

    void Start()
    {
        playerFsm = new HFSM<E_PlayerStateType, PlayerController>(this);

        playerFsm.AddState(E_PlayerStateType.Idle, new PlayerState_Idle());
        playerFsm.AddState(E_PlayerStateType.Move, new PlayerState_Move());

        playerFsm.SwitchState(E_PlayerStateType.Idle);
    }

    // Update is called once per frame
    void Update()
    {
        playerFsm.OnUpdate();
    }
}
