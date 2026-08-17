using CloudyWing.Enumeration.Abstractions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Modeling;

/// <summary>
/// 提供 Enumeration 與其基礎值型別之間的 EF Core 值轉換。
/// </summary>
/// <typeparam name="TEnum">Enumeration 型別。</typeparam>
/// <typeparam name="TValue">基礎值型別。</typeparam>
public sealed class EnumerationValueConverter<TEnum, TValue> : ValueConverter<TEnum, TValue>
    where TEnum : EnumerationBase<TEnum, TValue>
    where TValue : IComparable {
    /// <summary>
    /// 初始化 <see cref="EnumerationValueConverter{TEnum,TValue}" /> 類別的新執行個體。
    /// </summary>
    public EnumerationValueConverter()
        : base(
            enumeration => enumeration.Value,
            value => EnumerationBase<TEnum, TValue>.Parse(value)
        ) {
    }
}
