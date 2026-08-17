using CloudyWing.DomainKit.Resources;

namespace CloudyWing.DomainKit.Exceptions;

/// <summary>
/// 表示可預期的領域業務錯誤。
/// </summary>
public class BusinessException : Exception {
    /// <summary>
    /// 初始化 <see cref="BusinessException" /> 類別的新執行個體。
    /// </summary>
    public BusinessException()
        : base(DomainKitResources.BusinessException) {
        ErrorType = AppErrorType.BusinessRule;
    }

    /// <summary>
    /// 初始化 <see cref="BusinessException" /> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    public BusinessException(string message)
        : base(message) {
        ErrorType = AppErrorType.BusinessRule;
    }

    /// <summary>
    /// 初始化 <see cref="BusinessException" /> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    /// <param name="innerException">造成目前例外狀況的例外。</param>
    public BusinessException(string message, Exception innerException)
        : base(message, innerException) {
        ErrorType = AppErrorType.BusinessRule;
    }

    /// <summary>
    /// 初始化具有穩定分類與錯誤碼的業務例外。
    /// </summary>
    public BusinessException(string message, AppErrorType errorType, string? errorCode = null)
        : base(message) {
        if (errorType == AppErrorType.None) {
            throw new ArgumentOutOfRangeException(nameof(errorType), "業務例外不可使用 None 分類。");
        }

        ErrorType = errorType;
        ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode.Trim();
    }

    /// <summary>
    /// 取得穩定錯誤分類。
    /// </summary>
    public AppErrorType ErrorType { get; }

    /// <summary>
    /// 取得選配穩定錯誤碼。
    /// </summary>
    public string? ErrorCode { get; }
}
