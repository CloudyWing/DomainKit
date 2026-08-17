using CloudyWing.DomainKit.Resources;

namespace CloudyWing.DomainKit.Exceptions;

/// <summary>
/// 表示儲存作業發生樂觀鎖並行衝突。
/// </summary>
public class ConcurrencyConflictException : BusinessException {
    /// <summary>
    /// 初始化 <see cref="ConcurrencyConflictException" /> 類別的新執行個體。
    /// </summary>
    public ConcurrencyConflictException()
        : base(DomainKitResources.ConcurrencyConflict) {
    }

    /// <summary>
    /// 初始化 <see cref="ConcurrencyConflictException" /> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    public ConcurrencyConflictException(string message)
        : base(message) {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
    }

    /// <summary>
    /// 初始化 <see cref="ConcurrencyConflictException" /> 類別的新執行個體。
    /// </summary>
    /// <param name="innerException">造成目前例外狀況的例外。</param>
    public ConcurrencyConflictException(Exception innerException)
        : base(DomainKitResources.ConcurrencyConflict, innerException) {
        ArgumentNullException.ThrowIfNull(innerException);
    }

    /// <summary>
    /// 初始化 <see cref="ConcurrencyConflictException" /> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    /// <param name="innerException">造成目前例外狀況的例外。</param>
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException) {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(innerException);
    }
}
