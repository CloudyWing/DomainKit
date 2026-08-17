using CloudyWing.DomainKit.EntityFrameworkCore.Modeling;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.ModelBuilder;

[TestFixture]
public sealed class UtcDateTimeOffsetConverterTests {
    [Test]
    public void Convert_NonZeroOffset_NormalizesToUtcAndPreservesInstant() {
        UtcDateTimeOffsetConverter sut = new();
        DateTimeOffset source = new(2026, 6, 10, 15, 30, 0, TimeSpan.FromHours(8));

        DateTimeOffset stored = (DateTimeOffset)sut.ConvertToProvider(source)!;
        DateTimeOffset restored = (DateTimeOffset)sut.ConvertFromProvider(stored)!;

        using (Assert.EnterMultipleScope()) {
            Assert.That(stored.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(stored.UtcDateTime, Is.EqualTo(source.UtcDateTime));
            Assert.That(restored, Is.EqualTo(source));
            Assert.That(restored.Offset, Is.EqualTo(TimeSpan.Zero));
        }
    }
}
