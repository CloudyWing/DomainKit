namespace CloudyWing.DomainKit.EntityFrameworkCore.Modeling;

/// <summary>
/// 提供 DomainKit 具名全域查詢篩選的名稱。
/// </summary>
/// <remarks>
/// 只有呼叫 <see cref="QueryFilterModelBuilderExtensions.UseDomainKitNamedQueryFilters" /> 後，
/// DomainKit 才會以這些名稱註冊 EF Core 具名查詢篩選，可搭配 <c>IgnoreQueryFilters(filterKeys)</c> 個別略過。
/// </remarks>
public static class DomainKitQueryFilterNames {
    /// <summary>
    /// 軟刪除篩選的名稱。
    /// </summary>
    public const string SoftDelete = "DomainKit.SoftDelete";

    /// <summary>
    /// 租戶篩選的名稱。
    /// </summary>
    public const string Tenant = "DomainKit.Tenant";
}
