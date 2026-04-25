using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAbility<TOwner>
{

    public int priority;          // 优先级，决定打断关系
    public bool isUnlocked;       // 是否已解锁（用于能力解锁系统）
    public bool isActive;         // 当前是否正在执行

    protected TOwner owner;

    public void Init(TOwner owner)
    {
        this.owner = owner;
    }

    // 当前条件下能不能激活？
    public virtual bool CanActivate()
    {
        return isUnlocked && !isActive;
    }

    // 当前能不能被更高优先级打断？
    public virtual bool CanBeInterrupted()
    {
        return true;
    }

    //触发方法
    public virtual void Activate() { isActive = true; }

    //计时方法
    public virtual void Tick(float deltaTime) { }

    //退出方法
    public virtual void Deactivate() { isActive = false; }

}
