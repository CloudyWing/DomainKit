using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Modeling;

/// <summary>
/// 將 <see cref="DateTimeOffset" /> 在寫入資料庫時轉換為 UTC <see cref="DateTime" />，
/// 並於讀取時還原為 offset 為 0 的 <see cref="DateTimeOffset" />。
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTimeOffset, DateTime> {
    /// <summary>
    /// 初始化 <see cref="UtcDateTimeConverter" /> 類別的新執行個體。
    /// </summary>
    public UtcDateTimeConverter()
        : base(
            value => value.UtcDateTime,
            value => new DateTimeOffset(
                value.Kind == DateTimeKind.Utc
                    ? value
                    : DateTime.SpecifyKind(value, DateTimeKind.Utc)
            )) {
    }
}
