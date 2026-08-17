using CloudyWing.DomainKit.Tenancy;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Tenancy;

[TestFixture]
public sealed class AmbientCurrentTenantTests {
    [Test]
    public void Change_NestedScopes_RestoresParentTenant() {
        AmbientCurrentTenant currentTenant = new();
        Guid outerTenantId = Guid.CreateVersion7();
        Guid innerTenantId = Guid.CreateVersion7();

        using (currentTenant.Change(outerTenantId)) {
            Assert.That(currentTenant.TenantId, Is.EqualTo(outerTenantId));

            using (currentTenant.Change(innerTenantId)) {
                Assert.That(currentTenant.TenantId, Is.EqualTo(innerTenantId));
            }

            Assert.That(currentTenant.TenantId, Is.EqualTo(outerTenantId));
        }

        Assert.That(currentTenant.TenantId, Is.Null);
    }
}
