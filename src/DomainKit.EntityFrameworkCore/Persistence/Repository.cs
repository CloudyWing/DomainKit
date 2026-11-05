using System.Linq.Expressions;
using System.Text.Json;
using CloudyWing.DomainKit.Entities;
using CloudyWing.DomainKit.EntityFrameworkCore.Modeling;
using CloudyWing.DomainKit.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Persistence;

/// <summary>
/// 提供 EF Core 實體的基本新增、更新、刪除與查詢能力。
/// </summary>
/// <typeparam name="TDbContext">資料庫內容型別。</typeparam>
/// <typeparam name="TEntity">實體型別。</typeparam>
/// <typeparam name="TKey">查詢鍵型別。</typeparam>
public class Repository<TDbContext, TEntity, TKey>
    where TDbContext : DbContext
    where TEntity : class {
    private readonly TDbContext dbContext;
    private readonly IUnitOfWork unitOfWork;

    /// <summary>
    /// 初始化 <see cref="Repository{TDbContext,TEntity,TKey}" /> 類別的新執行個體。
    /// </summary>
    /// <param name="dbContext">資料庫內容。</param>
    /// <param name="unitOfWork">工作單元。</param>
    public Repository(TDbContext dbContext, IUnitOfWork unitOfWork) {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        this.dbContext = dbContext;
        this.unitOfWork = unitOfWork;
    }

    /// <summary>
    /// 取得查詢 Include 擴充點。
    /// </summary>
    protected virtual Func<IQueryable<TEntity>, IQueryable<TEntity>>? IncludeQuery => null;

    /// <summary>
    /// 新增實體。
    /// </summary>
    /// <param name="entity">要新增的實體。</param>
    /// <param name="autoCommit">若要立即提交則為 <see langword="true" />；否則為 <see langword="false" />。</param>
    /// <param name="actorId">審計使用者識別碼。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>新增後的實體。</returns>
    public async Task<TEntity> CreateAsync(
        TEntity entity,
        bool autoCommit = false,
        Guid? actorId = null,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(entity);

        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await CommitIfNeededAsync(autoCommit, actorId, cancellationToken).ConfigureAwait(false);

        return entity;
    }

    /// <summary>
    /// 更新指定鍵值的實體。
    /// </summary>
    /// <param name="key">查詢鍵值。</param>
    /// <param name="setup">更新作業。</param>
    /// <param name="autoCommit">若要立即提交則為 <see langword="true" />；否則為 <see langword="false" />。</param>
    /// <param name="actorId">審計使用者識別碼。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>更新後的實體。</returns>
    public async Task<TEntity> UpdateAsync(
        TKey key,
        Action<TEntity> setup,
        bool autoCommit = false,
        Guid? actorId = null,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(setup);

        TEntity entity = await GetAsync(key, cancellationToken).ConfigureAwait(false);
        setup(entity);
        await CommitIfNeededAsync(autoCommit, actorId, cancellationToken).ConfigureAwait(false);

        return entity;
    }

    /// <summary>
    /// 以非同步作業更新指定鍵值的實體。
    /// </summary>
    /// <param name="key">查詢鍵值。</param>
    /// <param name="setup">更新作業。</param>
    /// <param name="autoCommit">若要立即提交則為 <see langword="true" />；否則為 <see langword="false" />。</param>
    /// <param name="actorId">審計使用者識別碼。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>更新後的實體。</returns>
    public async Task<TEntity> UpdateAsync(
        TKey key,
        Func<TEntity, CancellationToken, Task> setup,
        bool autoCommit = false,
        Guid? actorId = null,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(setup);

        TEntity entity = await GetAsync(key, cancellationToken).ConfigureAwait(false);
        await setup(entity, cancellationToken).ConfigureAwait(false);
        await CommitIfNeededAsync(autoCommit, actorId, cancellationToken).ConfigureAwait(false);

        return entity;
    }

    /// <summary>
    /// 刪除指定鍵值的實體。
    /// </summary>
    /// <param name="key">查詢鍵值。</param>
    /// <param name="autoCommit">若要立即提交則為 <see langword="true" />；否則為 <see langword="false" />。</param>
    /// <param name="actorId">審計使用者識別碼。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>刪除或標記刪除後的實體。</returns>
    public async Task<TEntity> DeleteAsync(
        TKey key,
        bool autoCommit = false,
        Guid? actorId = null,
        CancellationToken cancellationToken = default
    ) {
        TEntity entity = await GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (entity is IHasDeletedTime) {
            dbContext.Entry(entity).Property(nameof(IHasDeletedTime.DeletedTime)).CurrentValue = DateTimeOffset.UtcNow;
            dbContext.Entry(entity).State = EntityState.Modified;
        } else {
            dbContext.Set<TEntity>().Remove(entity);
        }

        await CommitIfNeededAsync(autoCommit, actorId, cancellationToken).ConfigureAwait(false);

        return entity;
    }

    /// <summary>
    /// 查詢指定鍵值的實體。
    /// </summary>
    /// <param name="key">查詢鍵值。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>找到的實體，找不到時為 <see langword="null" />。</returns>
    public async Task<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default) {
        Expression<Func<TEntity, bool>> predicate = CreateKeyPredicate(key);

        return await CreateQuery()
            .FirstOrDefaultAsync(predicate, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 查詢指定鍵值的實體，找不到時拋出例外。
    /// </summary>
    /// <param name="key">查詢鍵值。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>找到的實體。</returns>
    /// <exception cref="EntityNotFoundException">找不到指定鍵值的實體時。</exception>
    public async Task<TEntity> GetAsync(TKey key, CancellationToken cancellationToken = default) {
        TEntity? entity = await FindAsync(key, cancellationToken).ConfigureAwait(false);

        return entity ?? throw new EntityNotFoundException(typeof(TEntity).Name, key ?? throw new ArgumentNullException(nameof(key)));
    }

    /// <summary>
    /// 建立鍵值查詢條件。
    /// </summary>
    /// <param name="key">查詢鍵值。</param>
    /// <returns>鍵值查詢條件。</returns>
    protected virtual Expression<Func<TEntity, bool>> CreateKeyPredicate(TKey key) {
        ArgumentNullException.ThrowIfNull(key);

        string propertyName = typeof(IHasExternalKey<TKey>).IsAssignableFrom(typeof(TEntity))
            ? nameof(IHasExternalKey<>.ExternalKey)
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

    private IQueryable<TEntity> CreateQuery() {
        IQueryable<TEntity> query = dbContext.Set<TEntity>();
        query = ApplyAggregateIncludes(query);

        return IncludeQuery is not null ? IncludeQuery(query) : query;
    }

    private IQueryable<TEntity> ApplyAggregateIncludes(IQueryable<TEntity> query) {
        IEntityType? entityType = dbContext.Model.FindEntityType(typeof(TEntity));
        if (entityType is null) {
            return query;
        }

        string? payload = entityType.FindAnnotation(EntityTypeBuilderExtensions.AggregateIncludesAnnotationName)
            ?.Value as string;
        if (string.IsNullOrWhiteSpace(payload)) {
            return query;
        }

        List<IncludeTreeNode>? nodes = JsonSerializer.Deserialize<List<IncludeTreeNode>>(payload);
        if (nodes is null || nodes.Count == 0) {
            return query;
        }

        foreach (string includePath in ExpandIncludePaths(nodes)) {
            query = query.Include(includePath);
        }

        return query;
    }

    private static IReadOnlyList<string> ExpandIncludePaths(IReadOnlyList<IncludeTreeNode> nodes) {
        List<string> paths = [];
        foreach (IncludeTreeNode node in nodes) {
            AppendIncludePaths(paths, node, prefix: null);
        }

        return paths;
    }

    private static void AppendIncludePaths(
        ICollection<string> paths,
        IncludeTreeNode node,
        string? prefix
    ) {
        string currentPath = string.IsNullOrEmpty(prefix)
            ? node.NavigationName
            : $"{prefix}.{node.NavigationName}";
        if (node.Children.Count == 0) {
            paths.Add(currentPath);

            return;
        }

        foreach (IncludeTreeNode child in node.Children) {
            AppendIncludePaths(paths, child, currentPath);
        }
    }

    private string GetPrimaryKeyPropertyName() {
        IEntityType entityType = dbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' is not part of the DbContext model.");
        IKey primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' does not define a primary key.");

        if (primaryKey.Properties.Count != 1) {
            throw new NotSupportedException($"Entity type '{typeof(TEntity).Name}' must define a single primary key.");
        }

        return primaryKey.Properties[0].Name;
    }

    private async Task CommitIfNeededAsync(bool autoCommit, Guid? actorId, CancellationToken cancellationToken) {
        if (!autoCommit) {
            return;
        }

        await unitOfWork.SaveChangesAsync(actorId, cancellationToken).ConfigureAwait(false);
    }
}
