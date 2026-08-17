using CloudyWing.DomainKit.EntityFrameworkCore.DependencyInjection;
using CloudyWing.DomainKit.EntityFrameworkCore.Events;
using CloudyWing.DomainKit.EntityFrameworkCore.Interceptors;
using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.Events;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Architecture;

[TestFixture]
public sealed class DependencyDirectionTests {
    [Test]
    public void CoreAssembly_References_DoesNotContainEntityFrameworkCore() {
        string[] referenceNames = typeof(IDomainEvent).Assembly
            .GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name ?? "")
            .ToArray();

        Assert.That(referenceNames, Does.Not.Contain("Microsoft.EntityFrameworkCore"));
    }

    [Test]
    public void PublicTypes_Namespaces_DoNotContainMoneyTrack() {
        IEnumerable<Type> types = typeof(IDomainEvent).Assembly.GetTypes()
            .Concat(typeof(UnitOfWork<>).Assembly.GetTypes());

        Assert.That(types.Any(type => (type.Namespace ?? "").StartsWith("CloudyWing.MoneyTrack", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public async Task AddDomainKit_WithHandlerAssembly_RegistersDispatchersHandlersAndUnitOfWorkAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync(registerHandlers: true);

        using (Assert.EnterMultipleScope()) {
            Assert.That(database.Services.GetRequiredService<IUnitOfWork>(), Is.Not.Null);
            Assert.That(database.Services.GetRequiredService<IDomainEventDispatcher>(), Is.TypeOf<DomainEventDispatcher>());
            Assert.That(database.Services.GetRequiredService<IIntegrationEventDispatcher>(), Is.TypeOf<IntegrationEventDispatcher>());
            Assert.That(database.Services.GetRequiredService<RowVersionStampInterceptor>(), Is.Not.Null);
            Assert.That(database.Services.GetServices<IDomainEventHandler<TestDomainEvent>>(), Is.Not.Empty);
            Assert.That(database.Services.GetServices<IIntegrationEventHandler<TestIntegrationEvent>>(), Is.Not.Empty);
        }
    }

    [Test]
    public void DomainKitOptions_Defaults_EnableInterceptorAndEmptyHandlerAssemblies() {
        DomainKitOptions options = new();

        using (Assert.EnterMultipleScope()) {
            Assert.That(options.EnableAuditAndDomainEventInterceptor, Is.True);
            Assert.That(options.HandlerAssemblies, Is.Empty);
        }
    }
}
