using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Boss/Boss01")]
public class CheckBossDerection_01 : Action
{
    public SharedBossController_01 boss;
    public SharedFloat distance;

    public override TaskStatus OnUpdate()
    {
        if (boss.Value == null) return TaskStatus.Failure;
        distance.Value = boss.Value.GetDistanceToPlayer();
        return TaskStatus.Success;

    }
}
