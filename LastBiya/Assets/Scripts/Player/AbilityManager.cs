using System.Collections;
using System.Collections.Generic;
using UnityEditor.Playables;
using UnityEngine;

/// <summary>
/// 能力管理器脚本
/// 其实例支持对Ability的注册，查找，修改以及激活。
/// </summary>
/// <typeparam name="TAbilityType">使用该管理器需要配套枚举，用来控制Ability的种类</typeparam>
/// <typeparam name="TOwner">该管理器的实际使用者</typeparam>
public class AbilityManager<TAbilityType,TOwner>
{
    //能力字典
    private Dictionary<TAbilityType, BaseAbility<TOwner>> abilityDic;
    //当前激活的能力列表
    private List<BaseAbility<TOwner>> activeAbilities = new();

    public TOwner Owner;

    public AbilityManager(TOwner owner)
    { 
        Owner = owner;
        abilityDic = new();
    }

    //向字典注册能力的方法
    public void AddAbilities(TAbilityType type,BaseAbility<TOwner> ability)
    {
        if (abilityDic.ContainsKey(type)) return;
        ability.Init(Owner);
        abilityDic.Add(type, ability);
    }

    /// <summary>
    /// 按枚举获取 Ability（返回基类类型）
    /// </summary>
    public BaseAbility<TOwner> Get(TAbilityType type)
    {
        abilityDic.TryGetValue(type, out var ability);
        return ability;
    }

    /// <summary>
    /// 按枚举获取 Ability 并转换为具体类型（用于护符修改参数等场景）
    /// 可以根据传入参数类型找到对应变量
    /// </summary>
    public T Get<T>(TAbilityType type) where T : BaseAbility<TOwner>
    {
        abilityDic.TryGetValue(type, out var ability);
        return ability as T;
    }

    /// <summary>
    /// 查询某个 Ability 是否正在执行
    /// </summary>
    public bool IsActive(TAbilityType type)
    {
        return abilityDic.TryGetValue(type, out var a) && a.isActive;
    }

    /// <summary>
    /// 尝试激活一个 Ability
    /// </summary>
    public bool TryActivate(TAbilityType type)
    {
        if (!abilityDic.TryGetValue(type, out var ability)) return false;

        // 第一步：问 Ability 自己能不能激活
        if (!ability.CanActivate()) return false;

        // 第二步：检查是否被更高优先级压制
        foreach (var active in activeAbilities)
        {
            // 有一个正在执行的 Ability 优先级 >= 我，并且它当前不可打断
            if (active.priority >= ability.priority && !active.CanBeInterrupted())
                return false;  // 激活失败
        }

        // 第三步：打断所有优先级更低的
        for (int i = activeAbilities.Count - 1; i >= 0; i--)
        {
            if (activeAbilities[i].priority < ability.priority)
            {
                activeAbilities[i].Deactivate();
                activeAbilities.RemoveAt(i);
            }
        }

        // 第四步：激活
        ability.Activate();
        activeAbilities.Add(ability);
        return true;
    }

    /// <summary>
    /// 每帧调用，驱动所有激活中的 Ability
    /// </summary>
    public void Tick(float deltaTime)
    {
        for (int i = activeAbilities.Count - 1; i >= 0; i--)
        {
            // 每个激活中的 Ability 执行自己的每帧逻辑
            activeAbilities[i].Tick(deltaTime);

            // 如果 Ability 在 Tick 中自己结束了（比如冲刺时间到了），从列表移除
            if (!activeAbilities[i].isActive)
                activeAbilities.RemoveAt(i);
        }
    }
}
    

