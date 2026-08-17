using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Modeling;

/// <summary>
/// 提供 EF Core 實體型別建置器的 DomainKit 擴充方法。
/// </summary>
public static class EntityTypeBuilderExtensions {
    internal const string AggregateIncludesAnnotationName = "DomainKit:AggregateIncludes";

    /// <summary>
    /// 設定聚合根於 Repository 查詢時自動套用的 Include 路徑。
    /// </summary>
    /// <typeparam name="TEntity">聚合根型別。</typeparam>
    /// <param name="builder">實體型別建置器。</param>
    /// <param name="configure">Include 路徑設定委派。</param>
    /// <returns>實體型別建置器。</returns>
    public static EntityTypeBuilder<TEntity> HasAggregateIncludes<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Action<AggregateIncludeBuilder<TEntity>> configure
    ) where TEntity : class {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        AggregateIncludeBuilder<TEntity> includeBuilder = new();
        configure(includeBuilder);

        string payload = JsonSerializer.Serialize(includeBuilder.Build());
        builder.Metadata.SetAnnotation(AggregateIncludesAnnotationName, payload);

        return builder;
    }
}
