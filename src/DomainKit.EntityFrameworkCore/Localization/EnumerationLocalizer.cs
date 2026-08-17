using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CloudyWing.Enumeration.Abstractions;
using Microsoft.Extensions.Localization;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Localization;

/// <summary>
/// 提供原生 <c>enum</c> 與 Enumeration 顯示名稱的本地化查詢。
/// </summary>
/// <typeparam name="TEnum">原生 <c>enum</c> 或 Enumeration 型別。</typeparam>
public sealed class EnumerationLocalizer<TEnum> where TEnum : notnull {
    private static readonly Func<TEnum, string> NameAccessor = CreateNameAccessor();
    private readonly IStringLocalizer localizer;

    /// <summary>
    /// 初始化 <see cref="EnumerationLocalizer{TEnum}" /> 類別的新執行個體。
    /// </summary>
    /// <param name="localizer">字串本地化器。</param>
    public EnumerationLocalizer(IStringLocalizer localizer) {
        ArgumentNullException.ThrowIfNull(localizer);

        this.localizer = localizer;
    }

    /// <summary>
    /// 取得 Enumeration 成員的本地化顯示名稱。
    /// </summary>
    /// <param name="enumeration">Enumeration 執行個體。</param>
    /// <returns>本地化顯示名稱。</returns>
    public string GetDisplayName([DisallowNull] TEnum enumeration) {
        ArgumentNullException.ThrowIfNull(enumeration);

        string name = NameAccessor(enumeration);
        if (typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false)
            && name.Contains(", ", StringComparison.Ordinal)) {
            return string.Join(", ", name.Split(", ", StringSplitOptions.None).Select(GetLocalizedName));
        }

        return GetLocalizedName(name);
    }

    private string GetLocalizedName(string name) {
        string key = $"{typeof(TEnum).Name}.{name}";
        LocalizedString result = localizer[key];

        return result.ResourceNotFound ? name : result.Value;
    }

    private static Func<TEnum, string> CreateNameAccessor() {
        if (typeof(TEnum).IsEnum) {
            return enumeration => enumeration.ToString()
                ?? throw new InvalidOperationException($"{typeof(TEnum).FullName} 無法取得成員名稱。");
        }

        if (!IsEnumerationType(typeof(TEnum))) {
            throw new InvalidOperationException(
                $"{typeof(TEnum).FullName} 必須為原生 enum 或繼承 EnumerationBase<,>。"
            );
        }

        PropertyInfo? nameProperty = typeof(TEnum).GetProperty(nameof(EnumerationNameProbe.Name));
        if (nameProperty is null || nameProperty.PropertyType != typeof(string) || nameProperty.GetMethod is null) {
            throw new InvalidOperationException($"{typeof(TEnum).FullName} 必須公開 string Name 屬性。");
        }

        return enumeration => (string)(nameProperty.GetValue(enumeration)
            ?? throw new InvalidOperationException($"{typeof(TEnum).FullName}.Name 不可為 null。"));
    }

    private static bool IsEnumerationType(Type type) {
        Type? current = type;
        while (current is not null && current != typeof(object)) {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(EnumerationBase<,>)) {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private sealed class EnumerationNameProbe : EnumerationBase<EnumerationNameProbe, int> {
        private EnumerationNameProbe(int value)
            : base(value) {
        }
    }
}
