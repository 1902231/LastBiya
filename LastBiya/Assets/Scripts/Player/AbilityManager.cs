using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �����������ű�
/// ��ʵ��֧�ֶ�Ability��ע�ᣬ���ң��޸��Լ����
/// </summary>
/// <typeparam name="TAbilityType">ʹ�øù�������Ҫ����ö�٣���������Ability������</typeparam>
/// <typeparam name="TOwner">�ù�������ʵ��ʹ����</typeparam>
public class AbilityManager<TAbilityType,TOwner>
{
    //�����ֵ�
    private Dictionary<TAbilityType, BaseAbility<TOwner>> abilityDic;
    //��ǰ����������б�
    private List<BaseAbility<TOwner>> activeAbilities = new();

    public TOwner Owner;

    public AbilityManager(TOwner owner)
    { 
        Owner = owner;
        abilityDic = new();
    }

    //���ֵ�ע�������ķ���
    public void AddAbilities(TAbilityType type,BaseAbility<TOwner> ability)
    {
        if (abilityDic.ContainsKey(type)) return;
        ability.Init(Owner);
        abilityDic.Add(type, ability);
    }

    /// <summary>
    /// ��ö�ٻ�ȡ Ability�����ػ������ͣ�
    /// </summary>
    public BaseAbility<TOwner> Get(TAbilityType type)
    {
        abilityDic.TryGetValue(type, out var ability);
        return ability;
    }

    /// <summary>
    /// ��ö�ٻ�ȡ Ability ��ת��Ϊ�������ͣ����ڻ����޸Ĳ����ȳ�����
    /// ���Ը��ݴ�����������ҵ���Ӧ����
    /// </summary>
    public T Get<T>(TAbilityType type) where T : BaseAbility<TOwner>
    {
        abilityDic.TryGetValue(type, out var ability);
        return ability as T;
    }

    /// <summary>
    /// ��ѯĳ�� Ability �Ƿ�����ִ��
    /// </summary>
    public bool IsActive(TAbilityType type)
    {
        return abilityDic.TryGetValue(type, out var a) && a.isActive;
    }

    /// <summary>
    /// ���Լ���һ�� Ability
    /// </summary>
    public bool TryActivate(TAbilityType type)
    {
        if (!abilityDic.TryGetValue(type, out var ability)) return false;

        // ��һ������ Ability �Լ��ܲ��ܼ���
        if (!ability.CanActivate()) return false;

        // �ڶ���������Ƿ񱻸������ȼ�ѹ��
        foreach (var active in activeAbilities)
        {
            // ��һ������ִ�е� Ability ���ȼ� >= �ң���������ǰ���ɴ��
            if (active.priority >= ability.priority && !active.CanBeInterrupted())
                return false;  // ����ʧ��
        }

        // ������������������ȼ����͵�
        for (int i = activeAbilities.Count - 1; i >= 0; i--)
        {
            if (activeAbilities[i].priority < ability.priority)
            {
                activeAbilities[i].Deactivate();
                activeAbilities.RemoveAt(i);
            }
        }

        // ���Ĳ�������
        ability.Activate();
        activeAbilities.Add(ability);
        return true;
    }

    /// <summary>
    /// ÿ֡���ã��������м����е� Ability
    /// </summary>
    public void Tick(float deltaTime)
    {
        for (int i = activeAbilities.Count - 1; i >= 0; i--)
        {
            // ÿ�������е� Ability ִ���Լ���ÿ֡�߼�
            activeAbilities[i].Tick(deltaTime);

            // ��� Ability �� Tick ���Լ������ˣ�������ʱ�䵽�ˣ������б��Ƴ�
            if (!activeAbilities[i].isActive)
                activeAbilities.RemoveAt(i);
        }
    }
}
    

