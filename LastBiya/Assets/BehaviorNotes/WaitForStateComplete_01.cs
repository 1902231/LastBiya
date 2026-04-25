using BehaviorDesigner.Runtime.Tasks;

/// <summary>
/// 等待 Boss 当前状态执行完成
/// 返回 Running 直到状态完成，完成后返回 Success
/// 用法：Sequence 中先 SwitchBossState 切状态，再接这个节点等待完成
/// </summary>
[TaskCategory("Boss/Boss01")]
public class WaitForStateComplete_01 : Action
{
    public SharedBossController_01 boss;

    public override TaskStatus OnUpdate()
    {
        if (boss.Value == null) return TaskStatus.Failure;
        return boss.Value.IsCurrentStateComplete() ? TaskStatus.Success : TaskStatus.Running;
    }
}
