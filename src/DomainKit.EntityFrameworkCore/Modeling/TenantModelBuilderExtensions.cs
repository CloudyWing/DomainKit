using System.Linq.Expressions;
using CloudyWing.DomainKit.EntityFrameworkCore.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Modeling;

/// <summary>
/// 提供多租戶 EF Core model convention。
/// </summary>
public static class TenantModelBuilderExtensions {
    private const string TenantPrimaryKeyAnnotation = "CloudyWing:TenantPrimaryKey";

    /// <summary>
    /// 標記啟用租戶 convention 後須改用租戶範圍業務主鍵的實體。
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="businessKeyPropertyNames">組成業務主鍵的屬性名稱。</param>
    /// <returns>The model builder.</returns>
    public static ModelBuilder HasTenantPrimaryKey<TEntity>(
        this ModelBuilder modelBuilder,
        params string[] businessKeyPropertyNames
    )
        where TEntity : class {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(businessKeyPropertyNames);
        if (businessKeyPropertyNames.Length == 0) {
            throw new ArgumentException("至少須指定一個業務主鍵屬性。", nameof(businessKeyPropertyNames));
        }

        modelBuilder.Entity<TEntity>().HasAnnotation(TenantPrimaryKeyAnnotation, businessKeyPropertyNames);
        return modelBuilder;
    }

    /// <summary>
    /// 對具有主鍵的非 owned 實體加入租戶欄位、查詢篩選與租戶前導索引。
    /// </summary>
    /// <remarks>
    /// 實體已有匿名查詢篩選時，租戶條件以 AND 合併至該匿名篩選。實體只有具名查詢篩選，
    /// 或模型先呼叫 <see cref="QueryFilterModelBuilderExtensions.UseDomainKitNamedQueryFilters" /> 時，
    /// 租戶條件以名稱 <see cref="DomainKitQueryFilterNames.Tenant" /> 註冊為具名查詢篩選；其餘情況註冊為匿名查詢篩選。
    /// </remarks>
    /// <param name="modelBuilder">Model 建置器。</param>
    /// <param name="tenantDbContext">提供目前租戶參數的 DbContext。</param>
    /// <param name="excludedEntityTypes">不套用多租戶 convention 的實體型別。</param>
    /// <returns>Model 建置器。</returns>
    public static ModelBuilder ApplyTenantConventions(
        this ModelBuilder modelBuilder,
        ITenantDbContext tenantDbContext,
        params Type[] excludedEntityTypes
    ) {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(tenantDbContext);
        HashSet<Type> exclusions = excludedEntityTypes.ToHashSet();
        bool useNamedFilters = QueryFilterModelBuilderExtensions.IsNamedQueryFiltersEnabled(modelBuilder.Model);
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes().ToList()) {
            if (entityType.IsOwned()
                || entityType.BaseType is not null
                || entityType.FindPrimaryKey() is null
                || exclusions.Contains(entityType.ClrType)) {
                continue;
            }

            IMutableProperty tenantProperty = entityType.FindProperty("TenantId")
                ?? entityType.AddProperty("TenantId", typeof(Guid));
            tenantProperty.IsNullable = false;
            ApplyTenantPrimaryKey(entityType, tenantProperty);
            PrependTenantToIndexes(entityType, tenantProperty);
            ApplyTenantFilter(entityType, CreateTenantFilter(entityType.ClrType, tenantDbContext), useNamedFilters);
        }

        return modelBuilder;
    }

    private static void ApplyTenantFilter(
        IMutableEntityType entityType,
        LambdaExpression tenantFilter,
        bool useNamedFilters
    ) {
        IReadOnlyCollection<IQueryFilter> declaredFilters = entityType.GetDeclaredQueryFilters();
        LambdaExpression? anonymousFilter = declaredFilters
            .SingleOrDefault(filter => filter.IsAnonymous)
            ?.Expression;
        if (anonymousFilter is not null) {
            // 既有匿名篩選無法與具名篩選並存，沿用合併為單一匿名篩選的做法。
            entityType.SetQueryFilter(CombineFilters(entityType.ClrType, anonymousFilter, tenantFilter));
        } else if (useNamedFilters || declaredFilters.Count > 0) {
            entityType.SetQueryFilter(DomainKitQueryFilterNames.Tenant, tenantFilter);
        } else {
            entityType.SetQueryFilter(tenantFilter);
        }
    }

    private static void ApplyTenantPrimaryKey(IMutableEntityType entityType, IMutableProperty tenantProperty) {
        if (entityType.FindAnnotation(TenantPrimaryKeyAnnotation)?.Value is not string[] propertyNames) {
            return;
        }

        IReadOnlyList<IMutableProperty> businessKeyProperties = propertyNames
            .Select(propertyName => entityType.FindProperty(propertyName)
                ?? throw new InvalidOperationException(
                    $"Entity '{entityType.DisplayName()}' does not define property '{propertyName}'."
                ))
            .ToList();
        IReadOnlyList<IMutableProperty> properties = [tenantProperty, .. businessKeyProperties];
        entityType.SetPrimaryKey(properties);
    }

    private static void PrependTenantToIndexes(IMutableEntityType entityType, IMutableProperty tenantProperty) {
        IReadOnlyList<IMutableIndex> indexes = entityType.GetIndexes()
            .Where(index => !index.Properties.Contains(tenantProperty))
            .ToList();
        foreach (IMutableIndex index in indexes) {
            IReadOnlyDictionary<string, object?> annotations = index.GetAnnotations()
                .ToDictionary(annotation => annotation.Name, annotation => annotation.Value, StringComparer.Ordinal);
            IReadOnlyList<IMutableProperty> properties = [tenantProperty, .. index.Properties];
            bool isUnique = index.IsUnique;
            entityType.RemoveIndex(index);
            IMutableIndex tenantIndex = entityType.AddIndex(properties);
            tenantIndex.IsUnique = isUnique;
            foreach (KeyValuePair<string, object?> annotation in annotations) {
                tenantIndex.SetAnnotation(annotation.Key, annotation.Value);
            }
        }
    }

    private static LambdaExpression CreateTenantFilter(Type entityType, ITenantDbContext tenantDbContext) {
        ParameterExpression parameter = Expression.Parameter(entityType, "entity");
        MethodCallExpression tenantProperty = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [typeof(Guid)],
            parameter,
            Expression.Constant("TenantId")
        );
        MemberExpression currentTenantId = Expression.Property(
            Expression.Constant(tenantDbContext),
            nameof(ITenantDbContext.CurrentTenantId)
        );
        UnaryExpression nullableTenantProperty = Expression.Convert(tenantProperty, typeof(Guid?));
        return Expression.Lambda(Expression.Equal(nullableTenantProperty, currentTenantId), parameter);
    }

    private static LambdaExpression CombineFilters(
        Type entityType,
        LambdaExpression left,
        LambdaExpression right
    ) {
        ParameterExpression parameter = Expression.Parameter(entityType, "entity");
        Expression leftBody = new ParameterReplacingVisitor(left.Parameters[0], parameter).Visit(left.Body)!;
        Expression rightBody = new ParameterReplacingVisitor(right.Parameters[0], parameter).Visit(right.Body)!;
        return Expression.Lambda(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    private sealed class ParameterReplacingVisitor : ExpressionVisitor {
        private readonly ParameterExpression source;
        private readonly ParameterExpression target;

        public ParameterReplacingVisitor(ParameterExpression source, ParameterExpression target) {
            this.source = source;
            this.target = target;
        }

        protected override Expression VisitParameter(ParameterExpression node) {
            return node == source ? target : base.VisitParameter(node);
        }
    }
}
