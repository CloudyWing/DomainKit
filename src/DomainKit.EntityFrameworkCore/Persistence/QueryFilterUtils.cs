using CloudyWing.DomainKit.EntityFrameworkCore.Modeling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Persistence;

internal static class QueryFilterUtils {
    private static readonly IReadOnlyCollection<string> SoftDeleteFilterKeys = [DomainKitQueryFilterNames.SoftDelete];

    public static IQueryable<TEntity> ApplyFilterSwitches<TEntity>(
        IQueryable<TEntity> query,
        DbContext dbContext,
        bool ignoreAllFilters,
        bool ignoreSoftDeleteFilterOnly
    ) where TEntity : class {
        if (ignoreAllFilters) {
            return query.IgnoreQueryFilters();
        }

        if (!ignoreSoftDeleteFilterOnly) {
            return query;
        }

        IReadOnlyEntityType? rootEntityType = dbContext.Model.FindEntityType(typeof(TEntity))?.GetRootType();
        if (rootEntityType is not null
            && rootEntityType.FindDeclaredQueryFilter(DomainKitQueryFilterNames.SoftDelete) is null
            && rootEntityType.GetDeclaredQueryFilters().Any(filter => filter.IsAnonymous)
        ) {
            // 匿名篩選可能同時包含軟刪除與租戶條件，無法只略過其中一部分。
            throw new InvalidOperationException(
                $"實體 {typeof(TEntity).Name} 的全域查詢篩選為匿名篩選，無法只略過軟刪除條件。"
                    + $"請在 OnModelCreating 開頭呼叫 {nameof(QueryFilterModelBuilderExtensions.UseDomainKitNamedQueryFilters)}。"
            );
        }

        return query.IgnoreQueryFilters(SoftDeleteFilterKeys);
    }
}
