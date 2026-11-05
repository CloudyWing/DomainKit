using System.Linq.Expressions;
using CloudyWing.DomainKit.EntityFrameworkCore.Modeling;
using CloudyWing.DomainKit.Entities;
using CloudyWing.DomainKit.Paging;
using CloudyWing.DomainKit.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Persistence;

/// <summary>
/// 以 EF Core <see cref="DbContext" /> 實作唯讀查詢服務。
/// </summary>
/// <typeparam name="TDbContext">資料庫內容型別。</typeparam>
/// <typeparam name="TEntity">實體型別。</typeparam>
public abstract class QueryService<TDbContext, TEntity> : IQueryService<TEntity>
    where TDbContext : DbContext
    where TEntity : class {
    /// <summary>
    /// 初始化 <see cref="QueryService{TDbContext,TEntity}" /> 類別的新執行個體。
    /// </summary>
    /// <param name="dbContext">資料庫內容。</param>
    protected QueryService(TDbContext dbContext) {
        ArgumentNullException.ThrowIfNull(dbContext);

        DbContext = dbContext;
    }

    /// <summary>
    /// 取得資料庫內容。
    /// </summary>
    protected TDbContext DbContext { get; }

    /// <inheritdoc />
    public bool IgnoreSoftDeletedFilter { get; set; }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default
    ) {
        IQueryable<TEntity> query = CreateQuery();
        if (predicate is not null) {
            query = query.Where(predicate);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default
    ) {
        IQueryable<TEntity> query = CreateQuery();
        if (predicate is not null) {
            query = query.Where(predicate);
        }

        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TOutput> GetSingleAsync<TOutput>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TOutput>> selector,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(selector);

        return await CreateQuery()
            .Where(predicate)
            .Select(selector)
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TOutput> GetSingleAsync<TOutput>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOutput>> selector,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(selector);

        return await SpecificationEvaluator<TEntity>
            .ApplyFilter(CreateQuery(), specification)
            .Select(selector)
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TOutput?> GetSingleOrDefaultAsync<TOutput>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TOutput>> selector,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(selector);

        return await CreateQuery()
            .Where(predicate)
            .Select(selector)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TOutput?> GetSingleOrDefaultAsync<TOutput>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOutput>> selector,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(selector);

        return await SpecificationEvaluator<TEntity>
            .ApplyFilter(CreateQuery(), specification)
            .Select(selector)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TOutput> GetFirstAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(selector);

        IQueryable<TEntity> query = ApplyPredicateAndOrder(CreateQuery(), predicate, orderBy);

        return await query
            .Select(selector)
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TOutput> GetFirstAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        ISpecification<TEntity> specification,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(specification);

        IQueryable<TEntity> query = SpecificationEvaluator<TEntity>.ApplyFilter(CreateQuery(), specification);
        if (orderBy is not null) {
            query = orderBy(query);
        }

        return await query
            .Select(selector)
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TOutput?> GetFirstOrDefaultAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(selector);

        IQueryable<TEntity> query = ApplyPredicateAndOrder(CreateQuery(), predicate, orderBy);

        return await query
            .Select(selector)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TOutput?> GetFirstOrDefaultAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        ISpecification<TEntity> specification,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(specification);

        IQueryable<TEntity> query = SpecificationEvaluator<TEntity>.ApplyFilter(CreateQuery(), specification);
        if (orderBy is not null) {
            query = orderBy(query);
        }

        return await query
            .Select(selector)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TOutput>> GetListAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(selector);

        IQueryable<TEntity> query = ApplyPredicateAndOrder(CreateQuery(), predicate, orderBy);

        return await query
            .Select(selector)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<PagedList<TOutput>> GetPagedListAsync<TOutput>(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, TOutput>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(selector);

        IQueryable<TEntity> query = ApplyPredicateAndOrder(CreateQuery(), predicate, orderBy);

        return query
            .Select(selector)
            .ToPagedListAsync(pageNumber, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(specification);

        return await SpecificationEvaluator<TEntity>
            .ApplyFilter(CreateQuery(), specification)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(specification);

        return await SpecificationEvaluator<TEntity>
            .ApplyFilter(CreateQuery(), specification)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TOutput>> GetListAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        ISpecification<TEntity> specification,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(specification);

        IQueryable<TEntity> query = SpecificationEvaluator<TEntity>.ApplyFilter(CreateQuery(), specification);
        if (orderBy is not null) {
            query = orderBy(query);
        }

        return await query
            .Select(selector)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PagedList<TOutput>> GetPagedListAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        IPagedSpecification<TEntity> specification,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(specification);

        if (!specification.HasPagination) {
            throw new ArgumentException("The paged specification must define pagination.", nameof(specification));
        }

        int totalCount = await SpecificationEvaluator<TEntity>
            .ApplyFilter(CreateQuery(), specification)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
        List<TOutput> items = await SpecificationEvaluator<TEntity>
            .ApplyPaged(CreateQuery(), specification)
            .Select(selector)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        PagingMetadata metadata = new(specification.PageNumber, specification.PageSize, totalCount);

        return new PagedList<TOutput>(items, metadata);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TOutput>> GetGroupedListAsync<TGroupKey, TOutput>(
        Expression<Func<TEntity, TGroupKey>> keySelector,
        Expression<Func<IGrouping<TGroupKey, TEntity>, TOutput>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TOutput, bool>>? havingPredicate = null,
        Func<IQueryable<TOutput>, IOrderedQueryable<TOutput>>? orderBy = null,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(selector);

        IQueryable<TOutput> query = ApplyGrouping(CreateQuery(), keySelector, selector, predicate, havingPredicate, orderBy);

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TOutput>> GetGroupedListAsync<TGroupKey, TOutput>(
        Expression<Func<TEntity, TGroupKey>> keySelector,
        Expression<Func<IGrouping<TGroupKey, TEntity>, TOutput>> selector,
        ISpecification<TEntity> specification,
        Expression<Func<TOutput, bool>>? havingPredicate = null,
        Func<IQueryable<TOutput>, IOrderedQueryable<TOutput>>? orderBy = null,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(specification);

        IQueryable<TOutput> query = ApplyGrouping(
            SpecificationEvaluator<TEntity>.ApplyFilter(CreateQuery(), specification),
            keySelector,
            selector,
            null,
            havingPredicate,
            orderBy
        );

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 建立查詢基底。
    /// </summary>
    /// <returns>查詢基底。</returns>
    protected virtual IQueryable<TEntity> CreateQuery() {
        IQueryable<TEntity> query = DbContext.Set<TEntity>().AsNoTracking();

        return IgnoreSoftDeletedFilter ? query.IgnoreQueryFilters() : query;
    }

    private static IQueryable<TEntity> ApplyPredicateAndOrder(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, bool>>? predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy
    ) {
        if (predicate is not null) {
            query = query.Where(predicate);
        }

        return orderBy is not null ? orderBy(query) : query;
    }

    private static IQueryable<TOutput> ApplyGrouping<TGroupKey, TOutput>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, TGroupKey>> keySelector,
        Expression<Func<IGrouping<TGroupKey, TEntity>, TOutput>> selector,
        Expression<Func<TEntity, bool>>? predicate,
        Expression<Func<TOutput, bool>>? havingPredicate,
        Func<IQueryable<TOutput>, IOrderedQueryable<TOutput>>? orderBy
    ) {
        if (predicate is not null) {
            query = query.Where(predicate);
        }

        IQueryable<TOutput> groupedQuery = query
            .GroupBy(keySelector)
            .Select(selector);

        if (havingPredicate is not null) {
            groupedQuery = groupedQuery.Where(havingPredicate);
        }

        return orderBy is not null ? orderBy(groupedQuery) : groupedQuery;
    }
}

/// <summary>
/// 提供依 Domain Key 查詢的唯讀查詢服務基底。
/// </summary>
/// <typeparam name="TDbContext">資料庫內容型別。</typeparam>
/// <typeparam name="TEntity">實體型別。</typeparam>
/// <typeparam name="TKey">查詢鍵型別。</typeparam>
public abstract class QueryService<TDbContext, TEntity, TKey> : QueryService<TDbContext, TEntity>
    where TDbContext : DbContext
    where TEntity : class {
    /// <summary>
    /// 初始化 <see cref="QueryService{TDbContext,TEntity,TKey}" /> 類別的新執行個體。
    /// </summary>
    /// <param name="dbContext">資料庫內容。</param>
    protected QueryService(TDbContext dbContext)
        : base(dbContext) {
    }

    /// <summary>
    /// 判斷指定鍵值的資料是否存在。
    /// </summary>
    /// <param name="key">查詢鍵值。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>若存在符合鍵值的資料則為 <see langword="true" />；否則為 <see langword="false" />。</returns>
    public Task<bool> AnyAsync(TKey key, CancellationToken cancellationToken = default) {
        return AnyAsync(CreateKeyPredicate(key), cancellationToken);
    }

    /// <summary>
    /// 取得指定鍵值的單筆投影結果。
    /// </summary>
    /// <typeparam name="TOutput">投影結果型別。</typeparam>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="key">查詢鍵值。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>投影結果。</returns>
    public Task<TOutput> GetAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        TKey key,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(selector);

        return GetSingleAsync(CreateKeyPredicate(key), selector, cancellationToken);
    }

    /// <summary>
    /// 取得指定鍵值的單筆投影結果，找不到時回傳 <see langword="null" />。
    /// </summary>
    /// <typeparam name="TOutput">投影結果型別。</typeparam>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="key">查詢鍵值。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>投影結果。</returns>
    public Task<TOutput?> FindAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        TKey key,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(selector);

        return GetSingleOrDefaultAsync(CreateKeyPredicate(key), selector, cancellationToken);
    }

    /// <summary>
    /// 建立 Domain Key 篩選條件。
    /// </summary>
    /// <param name="key">查詢鍵值。</param>
    /// <returns>鍵值篩選條件。</returns>
    protected virtual Expression<Func<TEntity, bool>> CreateKeyPredicate(TKey key) {
        ArgumentNullException.ThrowIfNull(key);

        string propertyName = typeof(IHasExternalKey<TKey>).IsAssignableFrom(typeof(TEntity))
            ? nameof(IHasExternalKey<TKey>.ExternalKey)
            : GetPrimaryKeyPropertyName();
        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "entity");
        MethodCallExpression property = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [typeof(TKey)],
            parameter,
            Expression.Constant(propertyName)
        );
        // 以 closure 捕捉鍵值，讓 EF Core 產生 SQL 參數並重用查詢計畫，避免每個鍵值都產生不同的 SQL 常值。
        Expression<Func<TKey>> keyValue = () => key;
        BinaryExpression equals = Expression.Equal(property, keyValue.Body);

        return Expression.Lambda<Func<TEntity, bool>>(equals, parameter);
    }

    private string GetPrimaryKeyPropertyName() {
        IEntityType entityType = DbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' is not part of the DbContext model.");
        IKey primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' does not define a primary key.");

        if (primaryKey.Properties.Count != 1) {
            throw new NotSupportedException($"Entity type '{typeof(TEntity).Name}' must define a single primary key.");
        }

        return primaryKey.Properties[0].Name;
    }
}
