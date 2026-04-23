using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 事件中心：全局公告板，任何脚本都可以订阅/触发事件，实现脚本间解耦通信。
/// 
/// 使用方式：
///   订阅（开始监听）：  EventCenter.Instance.AddEventListener("事件名", 回调方法);
///   触发（广播通知）：  EventCenter.Instance.EventTrigger("事件名");
///   取消订阅：         EventCenter.Instance.RemoveEventListener("事件名", 回调方法);
/// 
/// 带参数版本（比如传递一个 SkillNodeSO）：
///   订阅：  EventCenter.Instance.AddEventListener&lt;SkillNodeSO&gt;("事件名", 回调方法);
///   触发：  EventCenter.Instance.EventTrigger&lt;SkillNodeSO&gt;("事件名", 数据);
///   取消：  EventCenter.Instance.RemoveEventListener&lt;SkillNodeSO&gt;("事件名", 回调方法);
/// </summary>
public class EventCenter
{
    private static EventCenter instance;

    /// <summary>
    /// 事件字典：key=事件名，value=该事件的所有订阅者回调
    /// IEventInfo 是基类接口，实际存的是 EventInfo（无参）或 EventInfo&lt;T&gt;（带参）
    /// </summary>
    public Dictionary<string, IEventInfo> EventDic = new Dictionary<string, IEventInfo>();

    /// <summary>
    /// 单例访问，第一次访问时自动创建
    /// </summary>
    public static EventCenter Instance
    {
        get
        {
            if (instance == null)
                instance = new EventCenter();
            return instance;
        }
    }
    /// <summary>
    /// 订阅无参事件
    /// </summary>
    /// <param name="name">事件名（字符串，全局唯一标识）</param>
    /// <param name="action">回调方法（无参数的方法）</param>
    public void AddEventListener(string name, UnityAction action)
    {
        if (EventDic.ContainsKey(name))
        {
            // 事件已存在，把新的回调追加到委托链上（+=）
            (EventDic[name] as EventInfo).actions += action;
        }
        else
        {
            // 事件不存在，创建新的 EventInfo 并注册第一个回调
            EventDic.Add(name, new EventInfo(action));
        }
    }

    /// <summary>
    /// 触发无参事件：通知所有订阅者
    /// </summary>
    public void EventTrigger(string name)
    {
        if (EventDic.ContainsKey(name))
        {
            (EventDic[name] as EventInfo)?.actions?.Invoke();
        }
    }

    /// <summary>
    /// 取消订阅无参事件
    /// </summary>
    public void RemoveEventListener(string name, UnityAction action)
    {
        if (EventDic.ContainsKey(name))
        {
            (EventDic[name] as EventInfo).actions -= action;
        }
    }


    /// <summary>
    /// 订阅带参事件
    /// </summary>
    /// <typeparam name="T">参数类型（比如 SkillNodeSO）</typeparam>
    /// <param name="name">事件名</param>
    /// <param name="action">回调方法（接收一个 T 类型参数）</param>
    public void AddEventListener<T>(string name, UnityAction<T> action)
    {
        if (EventDic.ContainsKey(name))
        {
            // 【关键】转型为 EventInfo<T>，不是 EventInfo
            // 因为订阅时存的是 UnityAction<T>，取出来也必须是 EventInfo<T>
            (EventDic[name] as EventInfo<T>).actions += action;
        }
        else
        {
            EventDic.Add(name, new EventInfo<T>(action));
        }
    }

    /// <summary>
    /// 触发带参事件：通知所有订阅者，并传递数据
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="name">事件名</param>
    /// <param name="info">要传递给订阅者的数据</param>
    public void EventTrigger<T>(string name, T info)
    {
        // 【修复点1】原代码这里没有 info 参数，导致数据传不过去
        // 【修复点2】原代码转型成 EventInfo（无参版），应该是 EventInfo<T>
        if (EventDic.ContainsKey(name))
        {
            (EventDic[name] as EventInfo<T>)?.actions?.Invoke(info);
        }
    }

    /// <summary>
    /// 取消订阅带参事件
    /// </summary>
    public void RemoveEventListener<T>(string name, UnityAction<T> action)
    {
        // 【修复点3】原代码参数类型是 UnityAction（无参），应该是 UnityAction<T>
        // 【修复点4】原代码转型成 EventInfo，应该是 EventInfo<T>
        if (EventDic.ContainsKey(name))
        {
            (EventDic[name] as EventInfo<T>).actions -= action;
        }
    }

    /// <summary>
    /// 清空所有事件（场景切换时可以调用，防止残留订阅）
    /// </summary>
    public void Clear()
    {
        EventDic.Clear();
    }
}

/// <summary>
/// 基类接口，让无参和带参版本可以存在同一个 Dictionary 里
/// </summary>
public interface IEventInfo { }

/// <summary>
/// 无参事件容器：存储 UnityAction 委托链
/// </summary>/
public class EventInfo : IEventInfo
{
    public UnityAction actions;

    public EventInfo(UnityAction action)
    {
        actions += action;
    }
}

/// <summary>
/// 带参事件容器：存储 UnityAction&lt;T&gt; 委托链
/// T 可以是任意类型，比如 SkillNodeSO、int、string 等
/// </summary>
public class EventInfo<T> : IEventInfo
{
    public UnityAction<T> actions;

    public EventInfo(UnityAction<T> action)
    {
        actions += action;
    }
}
