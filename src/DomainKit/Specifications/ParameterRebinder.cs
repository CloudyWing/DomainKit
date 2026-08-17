using System.Linq.Expressions;

namespace CloudyWing.DomainKit.Specifications;

internal sealed class ParameterRebinder : ExpressionVisitor {
    private readonly IReadOnlyDictionary<ParameterExpression, ParameterExpression> replacements;

    private ParameterRebinder(IReadOnlyDictionary<ParameterExpression, ParameterExpression> replacements) {
        this.replacements = replacements;
    }

    public static Expression Replace(Expression expression, ParameterExpression source, ParameterExpression target) {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        return new ParameterRebinder(
            new Dictionary<ParameterExpression, ParameterExpression> {
                [source] = target
            }
        ).Visit(expression) ?? expression;
    }

    protected override Expression VisitParameter(ParameterExpression node) {
        return replacements.TryGetValue(node, out ParameterExpression? replacement)
            ? replacement
            : base.VisitParameter(node);
    }
}
