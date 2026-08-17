using System.Linq.Expressions;
using System.Reflection;
using CloudyWing.DomainKit.Entities;
using CloudyWing.Enumeration.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Modeling;

/// <summary>
/// 提供 DomainKit 實體的 EF Core 模型設定擴充方法。
/// </summary>
public static class ModelBuilderExtensions {
    /// <summary>
    /// 套用 DomainKit 的全域模型 convention。若宿主提供 <paramref name="dateTimeOffsetConverter" />，
    /// 則對所有 <see cref="DateTimeOffset" /> 與 <see cref="Nullable{DateTimeOffset}" /> 屬性套用該轉換器。
    /// </summary>
    /// <remarks>
    /// 由宿主 <see cref="Microsoft.EntityFrameworkCore.DbContext" /> 在覆寫 <c>ConfigureConventions</c> 時呼叫。
    /// <see cref="DateTimeOffset" /> 的儲存型別因資料庫而異（SQL Server 有原生 <c>datetimeoffset</c>，SQLite 無），
    /// 故 DomainKit 不內建特定 converter，改由宿主依 provider 傳入對應的 <see cref="ValueConverter" />（seam）。
    /// EF Core 會對 <see cref="Nullable{DateTimeOffset}" /> 自動套用同一轉換器。
    /// </remarks>
    /// <param name="configurationBuilder">模型設定建置器。</param>
    /// <param name="dateTimeOffsetConverter">宿主提供的 <see cref="DateTimeOffset" /> 值轉換器；為 <see langword="null" /> 時不套用轉換。</param>
    /// <returns>模型設定建置器。</returns>
    public static ModelConfigurationBuilder ApplyDomainKitConventions(
        this ModelConfigurationBuilder configurationBuilder,
        ValueConverter? dateTimeOffsetConverter = null
    ) {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        if (dateTimeOffsetConverter is not null) {
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion(dateTimeOffsetConverter.GetType());
        }

        return configurationBuilder;
    }

    /// <summary>
    /// 套用指定組件內所有 Enumeration 型別的值轉換慣例。
    /// </summary>
    /// <param name="configurationBuilder">模型設定建置器。</param>
    /// <param name="assembly">包含 Enumeration 型別的組件。</param>
    /// <returns>模型設定建置器。</returns>
    public static ModelConfigurationBuilder ApplyEnumerationConventions(
        this ModelConfigurationBuilder configurationBuilder,
        Assembly assembly
    ) {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        ArgumentNullException.ThrowIfNull(assembly);

        MethodInfo propertiesMethod = typeof(ModelConfigurationBuilder).GetMethod(
            nameof(ModelConfigurationBuilder.Properties),
            [typeof(Type)]
        ) ?? throw new InvalidOperationException("EF Core 未提供 ModelConfigurationBuilder.Properties(Type) API。");

        foreach (Type enumerationType in GetEnumerationTypes(assembly)) {
            Type converterType = typeof(EnumerationValueConverter<,>).MakeGenericType(
                enumerationType,
                GetEnumerationValueType(enumerationType)
            );
            object propertiesBuilder = propertiesMethod.Invoke(configurationBuilder, [enumerationType])
                ?? throw new InvalidOperationException($"無法建立 {enumerationType.Name} 的屬性設定建置器。");
            MethodInfo haveConversionMethod = propertiesBuilder.GetType().GetMethod("HaveConversion", [typeof(Type)])
                ?? throw new InvalidOperationException("EF Core 未提供 PropertiesConfigurationBuilder.HaveConversion(Type) API。");

            _ = haveConversionMethod.Invoke(propertiesBuilder, [converterType]);
        }

        return configurationBuilder;
    }

    /// <summary>
    /// 設定具備 <c>Id</c> 屬性的實體基底。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <param name="builder">實體型別建置器。</param>
    /// <returns>實體型別建置器。</returns>
    public static EntityTypeBuilder<TEntity> ConfigureEntityBase<TEntity>(
        this EntityTypeBuilder<TEntity> builder
    ) where TEntity : class {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(nameof(IEntity<>.Id));

        return builder;
    }

    /// <summary>
    /// 設定建立與更新時間欄位。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <param name="builder">實體型別建置器。</param>
    /// <returns>實體型別建置器。</returns>
    public static EntityTypeBuilder<TEntity> ConfigureAuditedEntity<TEntity>(
        this EntityTypeBuilder<TEntity> builder
    ) where TEntity : class, IHasCreatedTime, IHasUpdatedTime {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureEntityBase();
        builder.Property<DateTimeOffset>(nameof(IHasCreatedTime.CreatedTime)).IsRequired();
        builder.Property<DateTimeOffset>(nameof(IHasUpdatedTime.UpdatedTime)).IsRequired();

        return builder;
    }

    /// <summary>
    /// 設定建立時間欄位。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <param name="builder">實體型別建置器。</param>
    /// <returns>實體型別建置器。</returns>
    public static EntityTypeBuilder<TEntity> ConfigureCreationAuditedEntity<TEntity>(
        this EntityTypeBuilder<TEntity> builder
    ) where TEntity : class, IHasCreatedTime {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureEntityBase();
        builder.Property<DateTimeOffset>(nameof(IHasCreatedTime.CreatedTime)).IsRequired();

        return builder;
    }

    /// <summary>
    /// 設定建立時間與建立使用者欄位。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <param name="builder">實體型別建置器。</param>
    /// <returns>實體型別建置器。</returns>
    public static EntityTypeBuilder<TEntity> ConfigureCreationUserAuditedEntity<TEntity>(
        this EntityTypeBuilder<TEntity> builder
    ) where TEntity : class, IHasCreatedTime, IHasCreator {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureCreationAuditedEntity();
        builder.Property<Guid>(nameof(IHasCreator.CreatorId)).IsRequired();

        return builder;
    }

    /// <summary>
    /// 設定建立、更新時間與使用者欄位。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <param name="builder">實體型別建置器。</param>
    /// <returns>實體型別建置器。</returns>
    public static EntityTypeBuilder<TEntity> ConfigureUserAuditedEntity<TEntity>(
        this EntityTypeBuilder<TEntity> builder
    ) where TEntity : class, IHasCreatedTime, IHasUpdatedTime, IHasCreator, IHasUpdater {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureAuditedEntity();
        builder.Property<Guid>(nameof(IHasCreator.CreatorId)).IsRequired();
        builder.Property<Guid>(nameof(IHasUpdater.UpdaterId)).IsRequired();

        return builder;
    }

    /// <summary>
    /// 設定完整審計欄位與軟刪除全域查詢篩選。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <param name="builder">實體型別建置器。</param>
    /// <returns>實體型別建置器。</returns>
    public static EntityTypeBuilder<TEntity> ConfigureFullAuditedEntity<TEntity>(
        this EntityTypeBuilder<TEntity> builder
    ) where TEntity : class, IHasCreatedTime, IHasUpdatedTime, IHasCreator, IHasUpdater, IHasDeletedTime, IHasDeleter {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureUserAuditedEntity();
        builder.Property<DateTimeOffset?>(nameof(IHasDeletedTime.DeletedTime));
        builder.Property<Guid?>(nameof(IHasDeleter.DeleterId));
        builder.HasQueryFilter(CreateSoftDeleteFilter<TEntity>());

        return builder;
    }

    /// <summary>
    /// 設定樂觀鎖並行控制 token。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <param name="builder">實體型別建置器。</param>
    /// <returns>實體型別建置器。</returns>
    public static EntityTypeBuilder<TEntity> ConfigureRowVersion<TEntity>(
        this EntityTypeBuilder<TEntity> builder
    ) where TEntity : class, IHasRowVersion {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property<byte[]>(nameof(IHasRowVersion.RowVersion)).IsConcurrencyToken().IsRequired();

        return builder;
    }

    private static Expression<Func<TEntity, bool>> CreateSoftDeleteFilter<TEntity>() where TEntity : class {
        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "entity");
        MethodCallExpression deletedTime = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [typeof(DateTimeOffset?)],
            parameter,
            Expression.Constant(nameof(IHasDeletedTime.DeletedTime))
        );
        BinaryExpression notDeleted = Expression.Equal(
            deletedTime,
            Expression.Constant(null, typeof(DateTimeOffset?))
        );

        return Expression.Lambda<Func<TEntity, bool>>(notDeleted, parameter);
    }

    private static IReadOnlyList<Type> GetEnumerationTypes(Assembly assembly) {
        return assembly.DefinedTypes
            .Select(typeInfo => typeInfo.AsType())
            .Where(type => type is { IsClass: true, IsAbstract: false } && IsEnumerationType(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsEnumerationType(Type type) {
        Type? current = type;
        while (current is not null && current != typeof(object)) {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(EnumerationBase<,>)) {
                return current.GetGenericArguments()[0] == type;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static Type GetEnumerationValueType(Type type) {
        Type? current = type;
        while (current is not null && current != typeof(object)) {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(EnumerationBase<,>)) {
                Type[] arguments = current.GetGenericArguments();
                if (arguments[0] == type) {
                    return arguments[1];
                }
            }

            current = current.BaseType;
        }

        throw new InvalidOperationException($"型別 {type.FullName} 並非有效的 EnumerationBase<,> 子型別。");
    }
}
