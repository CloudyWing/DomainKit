using System.Linq.Expressions;

namespace CloudyWing.DomainKit.Specifications;

/// <summary>
/// 表示以條件運算式為基礎的不可變查詢規約。
/// </summary>
/// <typeparam name="T">規約目標型別。</typeparam>
public class Specification<T> : ISpecification<T> {
    private readonly Expression<Func<T, bool>> criteria;

    /// <summary>
    /// 初始化 <see cref="Specification{T}" /> 類別的新執行個體。
    /// </summary>
    /// <param name="criteria">篩選條件運算式。</param>
    public Specification(Expression<Func<T, bool>> criteria) {
        ArgumentNullException.ThrowIfNull(criteria);

        this.criteria = criteria;
    }

    /// <inheritdoc />
    public Expression<Func<T, bool>> ToExpression() => criteria;

    /// <inheritdoc />
    public ISpecification<T> And(ISpecification<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        return And(other.ToExpression());
    }

    /// <inheritdoc />
    public ISpecification<T> And(Expression<Func<T, bool>> other) {
        ArgumentNullException.ThrowIfNull(other);

        return new Specification<T>(Combine(other, Expression.AndAlso));
    }

    /// <inheritdoc />
    public ISpecification<T> Or(ISpecification<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        return Or(other.ToExpression());
    }

    /// <inheritdoc />
    public ISpecification<T> Or(Expression<Func<T, bool>> other) {
        ArgumentNullException.ThrowIfNull(other);

        return new Specification<T>(Combine(other, Expression.OrElse));
    }

    /// <inheritdoc />
    public ISpecification<T> Not() {
        ParameterExpression parameter = Expression.Parameter(typeof(T), "entity");
        Expression body = ParameterRebinder.Replace(criteria.Body, criteria.Parameters[0], parameter);

        return new Specification<T>(Expression.Lambda<Func<T, bool>>(Expression.Not(body), parameter));
    }

    private Expression<Func<T, bool>> Combine(
        Expression<Func<T, bool>> other,
        Func<Expression, Expression, BinaryExpression> combiner
    ) {
        ParameterExpression parameter = Expression.Parameter(typeof(T), "entity");
        Expression leftBody = ParameterRebinder.Replace(criteria.Body, criteria.Parameters[0], parameter);
        Expression rightBody = ParameterRebinder.Replace(other.Body, other.Parameters[0], parameter);

        return Expression.Lambda<Func<T, bool>>(combiner(leftBody, rightBody), parameter);
    }
}
