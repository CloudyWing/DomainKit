using System.Globalization;
using System.Resources;

namespace CloudyWing.DomainKit.Resources;

/// <summary>
/// 提供 DomainKit 預設例外訊息的資源存取。
/// </summary>
internal static class DomainKitResources {
    private static readonly ResourceManager ResourceManager = new(
        "CloudyWing.DomainKit.Resources.DomainKitResources",
        typeof(DomainKitResources).Assembly
    );

    /// <summary>
    /// 取得預設的業務錯誤訊息。
    /// </summary>
    public static string BusinessException => GetString(nameof(BusinessException), "A business rule was violated.");

    /// <summary>
    /// 取得預設的並行衝突訊息。
    /// </summary>
    public static string ConcurrencyConflict => GetString(
        nameof(ConcurrencyConflict),
        "The entity was modified by another operation."
    );

    /// <summary>
    /// 取得找不到實體的訊息範本。
    /// </summary>
    public static string EntityNotFound => GetString(
        nameof(EntityNotFound),
        "Entity '{0}' with key '{1}' was not found."
    );

    private static string GetString(string name, string fallback) {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? fallback;
    }
}
