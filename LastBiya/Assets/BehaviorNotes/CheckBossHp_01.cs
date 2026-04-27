using BehaviorDesigner.Runtime.Tasks;

//死亡判断节点
//boss血量百分比小于等于0，返回Failure，
//boss血量大于0，返回success
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
        return (hpPercent <= 0)
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }
}
