using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Persistence;

[TestFixture]
public sealed class RepositoryAggregateIncludeTests {
    [Test]
    public async Task FindAsync_HasAggregateIncludes_LoadsAnnotatedNestedCollectionsAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        Repository<TestDbContext, AggregateIncludeRoot, Guid> sut = new(database.DbContext, unitOfWork);
        AggregateIncludeRoot entity = CreateAggregate();
        database.DbContext.AggregateIncludeRoots.Add(entity);
        await unitOfWork.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();

        AggregateIncludeRoot actual = await sut.GetAsync(entity.Id);
        AggregateIncludeChild child = actual.Children.Single();

        using (Assert.EnterMultipleScope()) {
            Assert.That(database.DbContext.Entry(actual).Collection(root => root.Children).IsLoaded, Is.True);
            Assert.That(database.DbContext.Entry(child).Collection(item => item.Options).IsLoaded, Is.True);
            Assert.That(database.DbContext.Entry(child).Collection(item => item.ValidationRules).IsLoaded, Is.True);
            Assert.That(database.DbContext.Entry(actual).Collection(root => root.Tags).IsLoaded, Is.False);
            Assert.That(child.Options.Select(item => item.Name), Is.EquivalentTo(new[] { "Option-A" }));
            Assert.That(child.ValidationRules.Select(item => item.Name), Is.EquivalentTo(new[] { "Rule-A" }));
        }
    }

    [Test]
    public async Task FindAsync_HasAggregateIncludesAndIncludeQuery_LoadsBothConfiguredNavigationsAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        AggregateIncludeRepository sut = new(database.DbContext, unitOfWork);
        AggregateIncludeRoot entity = CreateAggregate();
        database.DbContext.AggregateIncludeRoots.Add(entity);
        await unitOfWork.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();

        AggregateIncludeRoot actual = await sut.GetAsync(entity.Id);
        AggregateIncludeChild child = actual.Children.Single();

        using (Assert.EnterMultipleScope()) {
            Assert.That(database.DbContext.Entry(actual).Collection(root => root.Children).IsLoaded, Is.True);
            Assert.That(database.DbContext.Entry(child).Collection(item => item.Options).IsLoaded, Is.True);
            Assert.That(database.DbContext.Entry(child).Collection(item => item.ValidationRules).IsLoaded, Is.True);
            Assert.That(database.DbContext.Entry(actual).Collection(root => root.Tags).IsLoaded, Is.True);
            Assert.That(actual.Tags.Select(item => item.Name), Is.EquivalentTo(new[] { "Tag-A" }));
        }
    }

    private static AggregateIncludeRoot CreateAggregate() {
        AggregateIncludeRoot root = new() {
            Name = "Root-A"
        };
        AggregateIncludeChild child = root.AddChild("Child-A");
        child.AddOption("Option-A");
        child.AddValidationRule("Rule-A");
        root.AddTag("Tag-A");

        return root;
    }

    private sealed class AggregateIncludeRepository : Repository<TestDbContext, AggregateIncludeRoot, Guid> {
        protected override Func<IQueryable<AggregateIncludeRoot>, IQueryable<AggregateIncludeRoot>>? IncludeQuery =>
            query => query.Include(entity => entity.Tags);

        public AggregateIncludeRepository(TestDbContext dbContext, IUnitOfWork unitOfWork)
            : base(dbContext, unitOfWork) {
        }
    }
}
