using System.Linq.Expressions;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Modeling;

/// <summary>
/// 收集聚合根自動 Include 路徑。
/// </summary>
/// <typeparam name="TEntity">聚合根型別。</typeparam>
public class AggregateIncludeBuilder<TEntity> where TEntity : class {
    private readonly Dictionary<string, AggregateIncludeNode> nodes;

    /// <summary>
    /// 初始化 <see cref="AggregateIncludeBuilder{TEntity}" /> 類別的新執行個體。
    /// </summary>
    public AggregateIncludeBuilder()
        : this(new Dictionary<string, AggregateIncludeNode>(StringComparer.Ordinal)) {
    }

    private AggregateIncludeBuilder(Dictionary<string, AggregateIncludeNode> nodes) {
        this.nodes = nodes;
    }

    /// <summary>
    /// 加入參考導覽屬性。
    /// </summary>
    /// <typeparam name="TProperty">導覽目標型別。</typeparam>
    /// <param name="navigationExpression">導覽屬性運算式。</param>
    /// <returns>目前的 Include 建置器。</returns>
    public AggregateIncludeBuilder<TEntity> Include<TProperty>(
        Expression<Func<TEntity, TProperty>> navigationExpression
    ) where TProperty : class {
        ArgumentNullException.ThrowIfNull(navigationExpression);

        AddInclude<TProperty>(GetNavigationName(navigationExpression.Body), thenConfigure: null);

        return this;
    }

    /// <summary>
    /// 加入集合導覽屬性。
    /// </summary>
    /// <typeparam name="TProperty">導覽目標型別。</typeparam>
    /// <param name="navigationExpression">導覽屬性運算式。</param>
    /// <returns>目前的 Include 建置器。</returns>
    public AggregateIncludeBuilder<TEntity> Include<TProperty>(
        Expression<Func<TEntity, IEnumerable<TProperty>>> navigationExpression
    ) where TProperty : class {
        ArgumentNullException.ThrowIfNull(navigationExpression);

        AddInclude<TProperty>(GetNavigationName(navigationExpression.Body), thenConfigure: null);

        return this;
    }

    /// <summary>
    /// 加入參考導覽屬性與其下層導覽。
    /// </summary>
    /// <typeparam name="TProperty">導覽目標型別。</typeparam>
    /// <param name="navigationExpression">導覽屬性運算式。</param>
    /// <param name="thenConfigure">下層導覽設定。</param>
    /// <returns>目前的 Include 建置器。</returns>
    public AggregateIncludeBuilder<TEntity> Include<TProperty>(
        Expression<Func<TEntity, TProperty>> navigationExpression,
        Action<AggregateIncludeBuilder<TProperty>>? thenConfigure
    ) where TProperty : class {
        ArgumentNullException.ThrowIfNull(navigationExpression);

        AddInclude(GetNavigationName(navigationExpression.Body), thenConfigure);

        return this;
    }

    /// <summary>
    /// 加入集合導覽屬性與其下層導覽。
    /// </summary>
    /// <typeparam name="TProperty">導覽目標型別。</typeparam>
    /// <param name="navigationExpression">導覽屬性運算式。</param>
    /// <param name="thenConfigure">下層導覽設定。</param>
    /// <returns>目前的 Include 建置器。</returns>
    public AggregateIncludeBuilder<TEntity> Include<TProperty>(
        Expression<Func<TEntity, IEnumerable<TProperty>>> navigationExpression,
        Action<AggregateIncludeBuilder<TProperty>>? thenConfigure
    ) where TProperty : class {
        ArgumentNullException.ThrowIfNull(navigationExpression);

        AddInclude(GetNavigationName(navigationExpression.Body), thenConfigure);

        return this;
    }

    internal IReadOnlyList<IncludeTreeNode> Build() {
        return nodes.Values
            .Select(node => node.ToImmutable())
            .ToList();
    }

    private void AddInclude<TProperty>(
        string navigationName,
        Action<AggregateIncludeBuilder<TProperty>>? thenConfigure
    ) where TProperty : class {
        AggregateIncludeNode node = GetOrAddNode(navigationName);
        if (thenConfigure is null) {
            return;
        }

        AggregateIncludeBuilder<TProperty> childBuilder = new(node.Children);
        thenConfigure(childBuilder);
    }

    private AggregateIncludeNode GetOrAddNode(string navigationName) {
        if (!nodes.TryGetValue(navigationName, out AggregateIncludeNode? node)) {
            node = new AggregateIncludeNode(navigationName);
            nodes.Add(navigationName, node);
        }

        return node;
    }

    private static string GetNavigationName(Expression expression) {
        Expression current = expression;
        while (current is UnaryExpression unaryExpression
            && (unaryExpression.NodeType == ExpressionType.Convert
                || unaryExpression.NodeType == ExpressionType.ConvertChecked)
        ) {
            current = unaryExpression.Operand;
        }

        return current is MemberExpression memberExpression
            ? memberExpression.Member.Name
            : throw new ArgumentException("導覽屬性運算式必須直接指向成員存取。", nameof(expression));
    }
}
