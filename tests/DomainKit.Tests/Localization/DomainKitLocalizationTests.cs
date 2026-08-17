using System.Globalization;
using CloudyWing.DomainKit.EntityFrameworkCore.Localization;
using CloudyWing.DomainKit.Exceptions;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.Extensions.Localization;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Localization;

[TestFixture]
public sealed class DomainKitLocalizationTests {
    [Test]
    public void BusinessException_DefaultConstructor_UsesLocalizedDefaultMessage() {
        BusinessException exception = new();

        Assert.That(exception.Message, Is.EqualTo("A business rule was violated."));
    }

    [Test]
    public void ConcurrencyConflictException_DefaultConstructor_UsesLocalizedDefaultMessage() {
        ConcurrencyConflictException exception = new();

        Assert.That(exception.Message, Is.EqualTo("The entity was modified by another operation."));
    }

    [Test]
    public void EntityNotFoundException_UsesLocalizedFormatString() {
        EntityNotFoundException exception = new("SalesOrder", "SO-001");

        Assert.That(exception.Message, Is.EqualTo("Entity 'SalesOrder' with key 'SO-001' was not found."));
    }

    [Test]
    public void EnumerationLocalizer_FoundResource_ReturnsLocalizedDisplayName() {
        EnumerationLocalizer<ActivityStatus> sut = new(
            new DictionaryStringLocalizer(
                new Dictionary<string, string> {
                    ["ActivityStatus.Active"] = "啟用"
                }
            )
        );

        string actual = sut.GetDisplayName(ActivityStatus.Active);

        Assert.That(actual, Is.EqualTo("啟用"));
    }

    [Test]
    public void EnumerationLocalizer_MissingResource_FallsBackToEnumerationName() {
        EnumerationLocalizer<ActivityStatus> sut = new(new DictionaryStringLocalizer(new Dictionary<string, string>()));

        string actual = sut.GetDisplayName(ActivityStatus.Archived);

        Assert.That(actual, Is.EqualTo("Archived"));
    }

    [Test]
    public void EnumerationLocalizer_NativeEnumFoundResource_ReturnsLocalizedDisplayName() {
        EnumerationLocalizer<NativeActivityStatus> sut = new(
            new DictionaryStringLocalizer(
                new Dictionary<string, string> {
                    ["NativeActivityStatus.Active"] = "啟用"
                }
            )
        );

        string actual = sut.GetDisplayName(NativeActivityStatus.Active);

        Assert.That(actual, Is.EqualTo("啟用"));
    }

    [Test]
    public void EnumerationLocalizer_NativeFlagsValue_LocalizesEachMember() {
        EnumerationLocalizer<NativePartnerRoles> sut = new(
            new DictionaryStringLocalizer(
                new Dictionary<string, string> {
                    ["NativePartnerRoles.Customer"] = "客戶",
                    ["NativePartnerRoles.Supplier"] = "供應商"
                }
            )
        );

        string actual = sut.GetDisplayName(NativePartnerRoles.Customer | NativePartnerRoles.Supplier);

        Assert.That(actual, Is.EqualTo("客戶, 供應商"));
    }

    private enum NativeActivityStatus {
        Active,
        Archived
    }

    [Flags]
    private enum NativePartnerRoles {
        Customer = 1,
        Supplier = 2
    }

    private sealed class DictionaryStringLocalizer : IStringLocalizer {
        private readonly IReadOnlyDictionary<string, string> values;

        public DictionaryStringLocalizer(IReadOnlyDictionary<string, string> values) {
            this.values = values;
        }

        public LocalizedString this[string name] {
            get {
                bool found = values.TryGetValue(name, out string? value);

                return new LocalizedString(name, found ? value! : name, !found);
            }
        }

        public LocalizedString this[string name, params object[] arguments] {
            get {
                bool found = values.TryGetValue(name, out string? value);
                string format = found ? value! : name;

                return new LocalizedString(
                    name,
                    string.Format(CultureInfo.CurrentCulture, format, arguments),
                    !found
                );
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) {
            return values.Select(pair => new LocalizedString(pair.Key, pair.Value, false));
        }

        public IStringLocalizer WithCulture(CultureInfo culture) {
            return this;
        }
    }
}
