/// <summary>
/// 通用单位接口，所有有血量的单位（玩家、敌人、Boss）实现此接口
/// 用于 UI 系统通过事件统一更新血条等显示
/// </summary>
public interface IUnit
{
    string UnitName { get; }
    float CurrentHP { get; }
    float MaxHP { get; }
    float CurrentPosture { get; }
    float MaxPosture { get; }
}
