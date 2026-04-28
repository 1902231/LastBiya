/// <summary>
/// Normal 状态：跟随玩家锚点，每帧检查目标队列
/// </summary>
public class BookState_Normal : HFSM_BaseState<E_BookStateType, BookCotroller>
{
    public override void OnUpdate()
    {
        // 队列非空 → 切到 Fighting
        if (owner.TargetQueue.Count > 0)
        {
            hfsm.SwitchState(E_BookStateType.Fighting);
        }
    }
}
