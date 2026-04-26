using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//检查boss躯干值节点
//boss躯干值小于0，返回Fauiler
[TaskCategory("Boss/Boss01")]
public class CheckBossPosature_01 : Conditional
{
    public SharedBossController_01 boss;

    public override TaskStatus OnUpdate()
    {
        if (boss.Value == null) return TaskStatus.Failure;
        return(boss.Value.currentPosture <= 0)
            ?TaskStatus.Success
            :TaskStatus.Failure;
    }
}
