namespace CloudyWing.DomainKit.Exceptions;

/// <summary>
/// 指定應用層可穩定對映的業務錯誤分類。
/// </summary>
public enum AppErrorType {
    /// <summary>
    /// 沒有錯誤。
    /// </summary>
    None,

    /// <summary>
    /// 輸入驗證失敗。
    /// </summary>
    Validation,

    /// <summary>
    /// 一般業務規則違反。
    /// </summary>
    BusinessRule,

    /// <summary>
    /// 目標不存在。
    /// </summary>
    NotFound,

    /// <summary>
    /// 並行、唯一鍵或狀態衝突。
    /// </summary>
    Conflict,

    /// <summary>
    /// 授權不足。
    /// </summary>
    Forbidden
}
