using System.Linq.Expressions;

namespace CloudyWing.DomainKit.Specifications;

/// <summary>
/// 提供查詢規約的條件式組合擴充方法。
/// </summary>
public static class SpecificationExtensions {
    /// <summary>
    /// 在條件成立時以 AND 組合另一個規約。
    /// </summary>
    /// <typeparam name="T">規約目標型別。</typeparam>
    /// <param name="specification">原始規約。</param>
    /// <param name="condition">是否套用組合。</param>
    /// <param name="other">要組合的規約。</param>
    /// <returns>組合後的規約。</returns>
    public static ISpecification<T> AndIf<T>(this ISpecification<T> specification, bool condition, ISpecification<T> other) {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(other);

        return condition ? specification.And(other) : specification;
    }

    /// <summary>
    /// 在條件成立時以 AND 組合另一個條件運算式。
    /// </summary>
    /// <typeparam name="T">規約目標型別。</typeparam>
    /// <param name="specification">原始規約。</param>
    /// <param name="condition">是否套用組合。</param>
    /// <param name="other">要組合的條件運算式。</param>
    /// <returns>組合後的規約。</returns>
    public static ISpecification<T> AndIf<T>(
        this ISpecification<T> specification,
        bool condition,
        Expression<Func<T, bool>> other
    ) {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(other);

        return condition ? specification.And(other) : specification;
    }

    /// <summary>
    /// 在條件成立時以 OR 組合另一個規約。
    /// </summary>
    /// <typeparam name="T">規約目標型別。</typeparam>
    /// <param name="specification">原始規約。</param>
    /// <param name="condition">是否套用組合。</param>
    /// <param name="other">要組合的規約。</param>
    /// <returns>組合後的規約。</returns>
    public static ISpecification<T> OrIf<T>(this ISpecification<T> specification, bool condition, ISpecification<T> other) {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(other);

        return condition ? specification.Or(other) : specification;
    }

    /// <summary>
    /// 在條件成立時以 OR 組合另一個條件運算式。
    /// </summary>
    /// <typeparam name="T">規約目標型別。</typeparam>
    /// <param name="specification">原始規約。</param>
    /// <param name="condition">是否套用組合。</param>
    /// <param name="other">要組合的條件運算式。</param>
    /// <returns>組合後的規約。</returns>
    public static ISpecification<T> OrIf<T>(
        this ISpecification<T> specification,
        bool condition,
        Expression<Func<T, bool>> other
    ) {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(other);

        return condition ? specification.Or(other) : specification;
    }

    /// <summary>
    /// 在值存在時以 AND 組合由該值建立的條件運算式。
    /// </summary>
    /// <typeparam name="T">規約目標型別。</typeparam>
    /// <typeparam name="TValue">條件值型別。</typeparam>
    /// <param name="specification">原始規約。</param>
    /// <param name="value">條件值。</param>
    /// <param name="otherFactory">依值建立條件運算式的工廠。</param>
    /// <returns>組合後的規約。</returns>
    public static ISpecification<T> AndIfHasValue<T, TValue>(
        this ISpecification<T> specification,
        TValue? value,
        Func<TValue, Expression<Func<T, bool>>> otherFactory
    ) {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(otherFactory);

        return HasValue(value) ? specification.And(otherFactory(value!)) : specification;
    }

    /// <summary>
    /// 在值存在時以 OR 組合由該值建立的條件運算式。
    /// </summary>
    /// <typeparam name="T">規約目標型別。</typeparam>
    /// <typeparam name="TValue">條件值型別。</typeparam>
    /// <param name="specification">原始規約。</param>
    /// <param name="value">條件值。</param>
    /// <param name="otherFactory">依值建立條件運算式的工廠。</param>
    /// <returns>組合後的規約。</returns>
    public static ISpecification<T> OrIfHasValue<T, TValue>(
        this ISpecification<T> specification,
        TValue? value,
        Func<TValue, Expression<Func<T, bool>>> otherFactory
    ) {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(otherFactory);

        return HasValue(value) ? specification.Or(otherFactory(value!)) : specification;
    }

    /// <summary>
    /// 以 AND 組合所有規約。
    /// </summary>
    /// <typeparam name="T">規約目標型別。</typeparam>
    /// <param name="specifications">要組合的規約集合。</param>
    /// <returns>組合後的規約；集合為空時回傳 <see cref="Specification{T}.True" />。</returns>
    /// <exception cref="ArgumentException">集合包含 <see langword="null" /> 元素時。</exception>
    public static ISpecification<T> AllOf<T>(this IEnumerable<ISpecification<T>> specifications) {
        ArgumentNullException.ThrowIfNull(specifications);

        return Aggregate(specifications, static (left, right) => left.And(right)) ?? Specification<T>.True;
    }

    /// <summary>
    /// 以 OR 組合所有規約。
    /// </summary>
    /// <typeparam name="T">規約目標型別。</typeparam>
    /// <param name="specifications">要組合的規約集合。</param>
    /// <returns>組合後的規約；集合為空時回傳 <see cref="Specification{T}.False" />。</returns>
    /// <exception cref="ArgumentException">集合包含 <see langword="null" /> 元素時。</exception>
    public static ISpecification<T> AnyOf<T>(this IEnumerable<ISpecification<T>> specifications) {
        ArgumentNullException.ThrowIfNull(specifications);

        return Aggregate(specifications, static (left, right) => left.Or(right)) ?? Specification<T>.False;
    }

    private static ISpecification<T>? Aggregate<T>(
        IEnumerable<ISpecification<T>> specifications,
        Func<ISpecification<T>, ISpecification<T>, ISpecification<T>> combiner
    ) {
        ISpecification<T>? combined = null;
        foreach (ISpecification<T>? specification in specifications) {
            if (specification is null) {
                throw new ArgumentException("規約集合不可包含 null 元素。", nameof(specifications));
            }

            combined = combined is null ? specification : combiner(combined, specification);
        }

        return combined;
    }

    private static bool HasValue<TValue>(TValue? value) {
        return value switch {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            _ => true
        };
    }
}
