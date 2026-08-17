using CloudyWing.DomainKit.EntityFrameworkCore.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class TestDatabase : IAsyncDisposable {
    private readonly SqliteConnection connection;
    private readonly ServiceProvider serviceProvider;
    private readonly IServiceScope scope;

    private TestDatabase(SqliteConnection connection, ServiceProvider serviceProvider, IServiceScope scope) {
        this.connection = connection;
        this.serviceProvider = serviceProvider;
        this.scope = scope;
        DbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
    }

    public TestDbContext DbContext { get; }

    public IServiceProvider Services => scope.ServiceProvider;

    public static async Task<TestDatabase> CreateAsync(
        bool registerHandlers = false,
        Action<IServiceCollection>? configureServices = null,
        Action<DbContextOptionsBuilder>? configureDbContext = null,
        Action<DomainKitOptions>? configureDomainKit = null
    ) {
        SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);

        ServiceCollection services = new();
        services.AddSingleton<EventRecorder>();
        services.AddLogging();
        services.AddDbContextFactory<TestDbContext>(
            builder => {
                builder.UseSqlite(connection);
                configureDbContext?.Invoke(builder);
            },
            ServiceLifetime.Scoped
        );
        configureServices?.Invoke(services);
        if (registerHandlers) {
            services.AddDomainKit<TestDbContext>(
                options => {
                    options.EnableOutboxDispatcher = true;
                    configureDomainKit?.Invoke(options);
                },
                typeof(TestDatabase).Assembly
            );
        } else {
            services.AddDomainKit<TestDbContext>();
        }

        ServiceProvider serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions {
                ValidateOnBuild = true,
                ValidateScopes = true
            }
        );
        IServiceScope scope = serviceProvider.CreateScope();
        TestDatabase database = new(connection, serviceProvider, scope);
        await database.DbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);

        return database;
    }

    public async ValueTask DisposeAsync() {
        await DbContext.DisposeAsync().ConfigureAwait(false);
        scope.Dispose();
        await serviceProvider.DisposeAsync().ConfigureAwait(false);
        await connection.DisposeAsync().ConfigureAwait(false);
    }
}
