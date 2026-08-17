using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Modeling;

/// <summary>
/// 將 <see cref="DateTimeOffset" /> 在寫入資料庫時正規化為 UTC（offset 0），確保時間一律以 UTC 儲存。
/// </summary>
public sealed class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset> {
    /// <summary>
    /// 初始化 <see cref="UtcDateTimeOffsetConverter" /> 類別的新執行個體。
    /// </summary>
    public UtcDateTimeOffsetConverter()
        : base(
            value => value.ToUniversalTime(),
            value => value) {
    }
}
