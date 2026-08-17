using CloudyWing.DomainKit.Exceptions;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Exceptions;

[TestFixture]
public sealed class BusinessExceptionTests {
    [Test]
    public void Constructor_DefaultClassification_UsesBusinessRule() {
        BusinessException sut = new("rule failed");

        Assert.Multiple(() => {
            Assert.That(sut.ErrorType, Is.EqualTo(AppErrorType.BusinessRule));
            Assert.That(sut.ErrorCode, Is.Null);
        });
    }

    [Test]
    public void EntityNotFoundException_UsesStableNotFoundClassification() {
        EntityNotFoundException sut = new("Customer", Guid.CreateVersion7());

        Assert.Multiple(() => {
            Assert.That(sut.ErrorType, Is.EqualTo(AppErrorType.NotFound));
            Assert.That(sut.ErrorCode, Is.EqualTo("entity.not-found"));
        });
    }
}
