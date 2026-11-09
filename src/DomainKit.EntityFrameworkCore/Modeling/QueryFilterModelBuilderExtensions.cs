using System.Runtime.CompilerServices;
using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Modeling;

/// <summary>
/// 提供 DomainKit 全域查詢篩選註冊方式的模型設定擴充方法。
/// </summary>
public static class QueryFilterModelBuilderExtensions {
    private static readonly ConditionalWeakTable<IReadOnlyModel, object> NamedQueryFilterModels = new();

    /// <summary>
    /// 讓後續的 DomainKit 模型設定改以 EF Core 具名查詢篩選註冊軟刪除與租戶篩選。
    /// </summary>
    /// <param name="modelBuilder">模型建置器。</param>
    /// <returns>模型建置器。</returns>
    /// <remarks>
    /// <para>
    /// 須在 <c>OnModelCreating</c> 開頭、呼叫 <see cref="ModelBuilderExtensions.ConfigureFullAuditedEntity{TEntity}" />
    /// 與 <see cref="TenantModelBuilderExtensions.ApplyTenantConventions" /> 之前呼叫，只影響之後的設定。
    /// 啟用後軟刪除篩選名稱為 <see cref="DomainKitQueryFilterNames.SoftDelete" />，租戶篩選名稱為 <see cref="DomainKitQueryFilterNames.Tenant" />，
    /// 可透過 <see cref="QueryService{TDbContext,TEntity}.IgnoreSoftDeleteFilterOnly" /> 只略過軟刪除篩選而保留租戶篩選。
    /// </para>
    /// <para>
    /// EF Core 不允許同一實體同時具有匿名與具名查詢篩選。啟用後，套用軟刪除篩選的實體不可再以 <c>HasQueryFilter(expression)</c>
    /// 加入匿名篩選，自訂條件請改用 <c>HasQueryFilter(name, expression)</c>。
    /// 未啟用時維持匿名查詢篩選，行為與既有版本相同。
    /// </para>
    /// </remarks>
    public static ModelBuilder UseDomainKitNamedQueryFilters(this ModelBuilder modelBuilder) {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        NamedQueryFilterModels.AddOrUpdate(modelBuilder.Model, true);

        return modelBuilder;
    }

    internal static bool IsNamedQueryFiltersEnabled(IReadOnlyModel model) {
        return NamedQueryFilterModels.TryGetValue(model, out _);
    }
}
