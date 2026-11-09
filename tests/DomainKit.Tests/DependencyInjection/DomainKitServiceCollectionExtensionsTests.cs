using System.Reflection;
using CloudyWing.DomainKit.EntityFrameworkCore.DependencyInjection;
using CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;
using CloudyWing.DomainKit.Events;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

    [Test]
    public void AddDomainKit_LegacyOverload_UsesDomainKitOptionsOutboxInstance() {
        ServiceCollection services = new();
        services.AddDomainKit<TestDbContext>(options => options.Outbox.BatchSize = 25);
        using ServiceProvider provider = services.BuildServiceProvider();

        OutboxOptions actual = provider.GetRequiredService<IOptions<OutboxOptions>>().Value;

        Assert.That(actual.BatchSize, Is.EqualTo(25));
    }

    [Test]
    public void AddDomainKit_OptionsBuilderOverload_AppliesDefaultsThenConfigurationBinding() {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Outbox:BatchSize"] = "10",
                ["Outbox:LockDuration"] = "00:05:00"
            })
            .Build();
        ServiceCollection services = new();
        services.AddDomainKit<TestDbContext>(
            options => {
                options.Outbox.BatchSize = 25;
                options.Outbox.MaxRetryCount = 3;
            },
            builder => builder.Bind(configuration.GetSection("Outbox"))
        );
        using ServiceProvider provider = services.BuildServiceProvider();

        OutboxOptions actual = provider.GetRequiredService<IOptions<OutboxOptions>>().Value;

        using (Assert.EnterMultipleScope()) {
            Assert.That(actual.BatchSize, Is.EqualTo(10));
            Assert.That(actual.LockDuration, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(actual.MaxRetryCount, Is.EqualTo(3));
        }
    }

    [Test]
    public void AddDomainKit_OptionsBuilderOverloadWithInvalidValue_ThrowsOptionsValidationException() {
        ServiceCollection services = new();
        services.AddDomainKit<TestDbContext>(
            configure: null,
            builder => builder.Configure(options => options.BatchSize = 0)
        );
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException? exception = Assert.Throws<OptionsValidationException>(
            () => _ = provider.GetRequiredService<IOptions<OutboxOptions>>().Value
        );

        Assert.That(exception!.Failures, Has.Some.Contains(nameof(OutboxOptions.BatchSize)));
    }

    [Test]
    public void AddDomainKit_OptionsBuilderOverloadWithValidateOnStart_FailsStartupValidation() {
        ServiceCollection services = new();
        services.AddDomainKit<TestDbContext>(
            configure: null,
            builder => builder
                .Configure(options => options.MaxRetryDelay = TimeSpan.FromSeconds(1))
                .ValidateOnStart()
        );
        using ServiceProvider provider = services.BuildServiceProvider();
        IStartupValidator validator = provider.GetRequiredService<IStartupValidator>();

        Assert.Throws<OptionsValidationException>(() => validator.Validate());
    }

    [Test]
    public void AddDomainKit_OptionsBuilderOverloadWithValidValues_PassesValidation() {
        ServiceCollection services = new();
        services.AddDomainKit<TestDbContext>(configure: null, builder => builder.ValidateOnStart());
        using ServiceProvider provider = services.BuildServiceProvider();
        IStartupValidator validator = provider.GetRequiredService<IStartupValidator>();

        Assert.DoesNotThrow(() => validator.Validate());
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
