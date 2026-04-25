using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 泛型分层状态机脚本
/// 其实例支持对状态的注册，查找，修改和使用
/// </summary>
/// <typeparam name="TKey">使用该管理器需要配套枚举，用来规范使用者state的种类(Tkey只能是值类型)</typeparam>
/// <typeparam name="TOwner">该管理器的实际使用者</typeparam>
public class HFSM<TKey,TOwner> where TKey : struct
{
    public Dictionary<TKey,HFSM_BaseState<TKey,TOwner>> stateDic;
    public TOwner owner;
    //public FsmParamator paramator;

    public HFSM_BaseState<TKey,TOwner> currentState;
    public HFSM(TOwner owner)
    { 
        this.owner = owner;
        stateDic = new();
    }

    public void AddState(TKey type, HFSM_BaseState<TKey, TOwner> state)
    {
        if (stateDic.ContainsKey(type)) return;
        state.Init(this, owner);
        stateDic.Add(type, state);
    }

    public void SwitchState(TKey type)
    {
        if (!stateDic.ContainsKey(type)) return;

        // 如果目标是父状态且有默认子状态，顺着 defaultChildType 走到叶子
        TKey finalType = type;
        while (stateDic.TryGetValue(finalType, out var s) && s.defaultChildType.HasValue)
        {
            finalType = s.defaultChildType.Value;
        }

        // 首次进入，没有当前状态，直接走完整进入链
        if (currentState == null)
        {
            HFSM_BaseState<TKey, TOwner> target = stateDic[finalType];
            List<HFSM_BaseState<TKey, TOwner>> enterPath = new List<HFSM_BaseState<TKey, TOwner>>();
            HFSM_BaseState<TKey, TOwner> s = target;
            while (s != null)
            {
                enterPath.Add(s);
                s = GetParent(s);
            }
            enterPath.Reverse();
            foreach (HFSM_BaseState<TKey, TOwner> state in enterPath)
            {
                state.OnEnter();
            }
            currentState = target;
            return;
        }

        //
        HFSM_BaseState<TKey, TOwner> CommonParent = FindCommonParent(currentState, stateDic[finalType]);

        //
        HFSM_BaseState<TKey, TOwner> tempState = currentState;
        while (tempState != null && tempState != CommonParent)
        {
            tempState.OnExit();
            tempState = GetParent(tempState);
        }

        // 进入目标状态
        tempState = stateDic[finalType];
        List<HFSM_BaseState<TKey, TOwner>> enterPath2 = new List<HFSM_BaseState<TKey, TOwner>>();
        while (tempState != null && tempState != CommonParent)
        { 
            enterPath2.Add(tempState);
            tempState = GetParent(tempState);
        }
        enterPath2.Reverse();
        foreach (HFSM_BaseState<TKey, TOwner> state in enterPath2)
        { 
            state.OnEnter();
        }

        //
        currentState = stateDic[finalType];
    }

    public void OnUpdate()
    {
        //currentState?.OnUpdate();

        //查找链，使用链上所有状态的update
        List<HFSM_BaseState<TKey, TOwner>> chain = new();
        HFSM_BaseState<TKey, TOwner> tempState = currentState;
        while (tempState != null)
        {
            chain.Add(tempState);
            tempState = GetParent(tempState);
        }
        chain.Reverse();

        foreach (HFSM_BaseState<TKey, TOwner> state in chain)
        {
            state.OnUpdate();
        }
    }

    public void OnFixedUpdate()
    {
        //currentState?.OnUpdate();

        //查找链，使用链上所有状态的update
        List<HFSM_BaseState<TKey, TOwner>> chain = new();
        HFSM_BaseState<TKey, TOwner> tempState = currentState;
        while (tempState != null)
        {
            chain.Add(tempState);
            tempState = GetParent(tempState);
        }
        chain.Reverse();

        foreach (HFSM_BaseState<TKey, TOwner> state in chain)
        {
            state.OnFixedUpdate();
        }
    }

    /// <summary>
    /// 查找公共父状态的方法
    /// </summary>
    /// <param name="currentState">当前状态</param>
    /// <param name="targetState">目标状态</param>
    /// <returns></returns>
    private HFSM_BaseState<TKey, TOwner> FindCommonParent(HFSM_BaseState<TKey, TOwner> currentState, HFSM_BaseState<TKey, TOwner> targetState)
    {
        List<HFSM_BaseState<TKey, TOwner>> ancestors = new List<HFSM_BaseState<TKey, TOwner>>();
        HFSM_BaseState<TKey, TOwner> tempState = currentState;
        while (tempState != null)
        {
            ancestors.Add(tempState);
            tempState = GetParent(tempState);
        }

        tempState = targetState;
        while (tempState != null)
        {
            if (ancestors.Contains(tempState)) return tempState;
            tempState = GetParent(tempState);
        }
        return null;
    }

    private HFSM_BaseState<TKey, TOwner> GetParent(HFSM_BaseState<TKey, TOwner> state)
    {
        //if (EqualityComparer<TKey>.Default.Equals(state.parentType, noneKey)) return null;
        if (!state.parentType.HasValue) return null;
        stateDic.TryGetValue(state.parentType.Value, out var parent);
        return parent;
    }
}
