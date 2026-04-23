using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class HFSM_BaseState<TKey,TOwner> where TKey : struct
{
    #region 非泛型逻辑
    /*
    //public BaseState parent = null;
    //public BaseState defaultChildState = null;

    //父状态和默认子状态对应的类型
    //public E_PlayerStateType parentType = E_PlayerStateType.None;
    //public E_PlayerStateType defaultChildType = E_PlayerStateType.None;

    protected Tkey parentType;
    protected Tkey defultChildType;

    protected TOwner owner;
    protected HFSM hfsm;

    public void Init(HFSM hfsm)
    {
        this.hfsm = hfsm;
        this.owner = 
    }

    public virtual void OnEnter() { }

    public virtual void OnUpdate() { }

    public virtual void OnExit() { }
    
    */
    #endregion

    public TKey? parentType = null;
    public TKey? defaultChildType = null;

    protected HFSM<TKey, TOwner> hfsm;
    protected TOwner owner;


    public void Init(HFSM<TKey, TOwner> hfsm, TOwner owner)
    {
        this.hfsm = hfsm;
        this.owner = owner;
    }

    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnFixedUpdate() { }
    public virtual void OnExit() { }
}
