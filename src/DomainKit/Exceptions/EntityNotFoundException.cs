using System.Globalization;
using CloudyWing.DomainKit.Resources;

namespace CloudyWing.DomainKit.Exceptions;

/// <summary>
/// 表示以指定鍵值查詢實體但找不到資料。
/// </summary>
public class EntityNotFoundException : BusinessException {
    /// <summary>
    /// 初始化 <see cref="EntityNotFoundException" /> 類別的新執行個體。
    /// </summary>
    /// <param name="entityName">實體名稱。</param>
    /// <param name="key">查詢鍵值。</param>
    public EntityNotFoundException(string entityName, object key)
        : base(
            string.Format(CultureInfo.CurrentCulture, DomainKitResources.EntityNotFound, entityName, key),
            AppErrorType.NotFound,
            "entity.not-found"
        ) {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentNullException.ThrowIfNull(key);

        EntityName = entityName;
        Key = key;
    }

    /// <summary>
    /// 取得實體名稱。
    /// </summary>
    public string EntityName { get; }

    /// <summary>
    /// 取得查詢鍵值。
    /// </summary>
    public object Key { get; }
}
