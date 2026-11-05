using CloudyWing.DomainKit.EntityFrameworkCore.Modeling;
using CloudyWing.DomainKit.Specifications;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Persistence;

internal static class SpecificationEvaluator<TEntity> where TEntity : class {
    public static IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, ISpecification<TEntity> specification) {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);

        return query.Where(specification.ToExpression());
    }

    public static IQueryable<TEntity> ApplyPaged(IQueryable<TEntity> query, IPagedSpecification<TEntity> specification) {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);

        IQueryable<TEntity> filteredQuery = ApplyFilter(query, specification);
        IQueryable<TEntity> orderedQuery = specification.OrderBy(filteredQuery);

        if (!specification.HasPagination) {
            return orderedQuery;
        }

        return orderedQuery
            .Skip(PagingUtils.GetSkipCount(specification.PageNumber, specification.PageSize))
            .Take(specification.PageSize);
    }
}
