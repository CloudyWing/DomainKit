using System.Reflection;
using CloudyWing.DomainKit.EntityFrameworkCore.DependencyInjection;
using CloudyWing.DomainKit.Events;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.DependencyInjection;

[TestFixture]
public sealed class DomainKitServiceCollectionExtensionsTests {
    [Test]
    public void AddDomainKit_AssemblyThrowsReflectionTypeLoadException_RegistersLoadableHandlers() {
        ServiceCollection services = new();
        PartiallyLoadableAssembly assembly = new([typeof(TestDomainEventHandler), null]);

        services.AddDomainKit<TestDbContext>(configure: null, assembly);

        Assert.That(
            services.Any(descriptor => descriptor.ServiceType == typeof(IDomainEventHandler<TestDomainEvent>)
                && descriptor.ImplementationType == typeof(TestDomainEventHandler)),
            Is.True
        );
    }

    private sealed class PartiallyLoadableAssembly : Assembly {
        private readonly Type?[] loadableTypes;

        public PartiallyLoadableAssembly(Type?[] loadableTypes) {
            this.loadableTypes = loadableTypes;
        }

        public override Type[] GetTypes() {
            throw new ReflectionTypeLoadException(loadableTypes, [new FileNotFoundException("Missing dependency.")]);
        }
    }
}
