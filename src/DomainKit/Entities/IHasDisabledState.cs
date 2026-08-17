namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 取得停用狀態。
/// </summary>
public interface IHasDisabledState {
    /// <summary>
    /// 取得一個值，用以指出是否停用。
    /// </summary>
    bool IsDisabled { get; }
}
