using System.Linq.Expressions;

namespace CloudyWing.DomainKit.Specifications;

/// <summary>
/// 定義可組合的查詢規約。
/// </summary>
/// <typeparam name="T">規約目標型別。</typeparam>
public interface ISpecification<T> {
    /// <summary>
    /// 建立規約對應的篩選條件運算式。
    /// </summary>
    /// <returns>篩選條件運算式。</returns>
    Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// 以 AND 與另一個規約組合目前規約。
    /// </summary>
    /// <param name="other">要組合的規約。</param>
    /// <returns>組合後的規約。</returns>
    ISpecification<T> And(ISpecification<T> other);

    /// <summary>
    /// 以 AND 與另一個條件運算式組合目前規約。
    /// </summary>
    /// <param name="other">要組合的條件運算式。</param>
    /// <returns>組合後的規約。</returns>
    ISpecification<T> And(Expression<Func<T, bool>> other);

    /// <summary>
    /// 以 OR 與另一個規約組合目前規約。
    /// </summary>
    /// <param name="other">要組合的規約。</param>
    /// <returns>組合後的規約。</returns>
    ISpecification<T> Or(ISpecification<T> other);

    /// <summary>
    /// 以 OR 與另一個條件運算式組合目前規約。
    /// </summary>
    /// <param name="other">要組合的條件運算式。</param>
    /// <returns>組合後的規約。</returns>
    ISpecification<T> Or(Expression<Func<T, bool>> other);

    /// <summary>
    /// 建立目前規約的補集規約。
    /// </summary>
    /// <returns>補集規約。</returns>
    ISpecification<T> Not();
}
