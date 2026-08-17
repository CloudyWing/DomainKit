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

    private static bool HasValue<TValue>(TValue? value) {
        return value switch {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            _ => true
        };
    }
}
