using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Boss/Boss01")]
public class CheckBossHp_01 : Conditional
{
    public SharedBossController_01 boss;
    public float minHP = 0f;
    public float maxHP = 1f;

    public override TaskStatus OnUpdate()
    {
        if (boss.Value == null) return TaskStatus.Failure;
        float hpPercent = boss.Value.GetHPPercentage();
        return (hpPercent >= minHP && hpPercent <= maxHP)
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }
}
