using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Boss/Boss01")]

public class SwitchBossState_01 : Action
{
    public SharedBossController_01 boss;
    public E_BossStateType_01 TaegetBossState;

    public override TaskStatus OnUpdate()
    {
        if (boss.Value == null) return TaskStatus.Failure;
        boss.Value.SwitchToState(TaegetBossState);
        return TaskStatus.Success;
    }
}


[System.Serializable]
public class SharedBossController_01 : SharedVariable<Boss01>

{

    public static implicit operator SharedBossController_01(Boss01 value)

    {

        return new SharedBossController_01 { Value = value };

    }

}
