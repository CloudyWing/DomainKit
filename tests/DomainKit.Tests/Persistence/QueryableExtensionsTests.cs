using CloudyWing.DomainKit.EntityFrameworkCore.Modeling;
using CloudyWing.DomainKit.Paging;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Persistence;

[TestFixture]
public sealed class QueryableExtensionsTests {
    [Test]
    public async Task ToPagedListAsync_PageOffsetExceedsInt32_ReturnsEmptyPageAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        await SeedAsync(database, "A", "B", "C");

        PagedList<string> actual = await database.DbContext.HardDeleteEntities
            .OrderBy(entity => entity.Name)
            .Select(entity => entity.Name)
            .ToPagedListAsync(int.MaxValue, 100);

        using (Assert.EnterMultipleScope()) {
            Assert.That(actual.Items, Is.Empty);
            Assert.That(actual.Metadata.TotalCount, Is.EqualTo(3));
        }
    }

    [Test]
    public async Task ToKeysetPageAsync_FirstPage_ReturnsOrderedItemsAndNextKeyAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        await SeedAsync(database, "C", "A", "B");

        KeysetPage<string, string> actual = await database.DbContext.HardDeleteEntities
            .Select(entity => entity.Name)
            .ToKeysetPageAsync(name => name, 2);

        using (Assert.EnterMultipleScope()) {
            Assert.That(actual.Items, Is.EqualTo(new[] { "A", "B" }));
            Assert.That(actual.NextKey, Is.EqualTo("B"));
            Assert.That(actual.HasMore, Is.True);
        }
    }

    [Test]
    public async Task ToKeysetPageAsync_AfterKey_ReturnsRemainingItemsAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        await SeedAsync(database, "C", "A", "B");

        KeysetPage<string, string> actual = await database.DbContext.HardDeleteEntities
            .Select(entity => entity.Name)
            .ToKeysetPageAsync(name => name, "B", 2);

        using (Assert.EnterMultipleScope()) {
            Assert.That(actual.Items, Is.EqualTo(new[] { "C" }));
            Assert.That(actual.NextKey, Is.EqualTo("C"));
            Assert.That(actual.HasMore, Is.False);
        }
    }

    [Test]
    public async Task ToKeysetPageAsync_NullAfterKey_StartsFromFirstItemAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        await SeedAsync(database, "B", "A");

        KeysetPage<string, string> actual = await database.DbContext.HardDeleteEntities
            .Select(entity => entity.Name)
            .ToKeysetPageAsync(name => name, (string?)null, 5);

        Assert.That(actual.Items, Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public async Task ToKeysetPageAsync_IterateGuidKeys_VisitsEveryRowOnceAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        await SeedAsync(database, "A", "B", "C", "D", "E");
        List<Guid> visited = [];

        KeysetPage<HardDeleteEntity, Guid> page = await database.DbContext.HardDeleteEntities
            .AsNoTracking()
            .ToKeysetPageAsync(entity => entity.Id, 2);
        visited.AddRange(page.Items.Select(entity => entity.Id));
        while (page.HasMore) {
            page = await database.DbContext.HardDeleteEntities
                .AsNoTracking()
                .ToKeysetPageAsync(entity => entity.Id, page.NextKey, 2);
            visited.AddRange(page.Items.Select(entity => entity.Id));
        }

        List<Guid> expected = await database.DbContext.HardDeleteEntities
            .Select(entity => entity.Id)
            .ToListAsync();
        Assert.That(visited, Is.EquivalentTo(expected));
    }

    [Test]
    public async Task ToKeysetPageAsync_EmptySource_ReturnsEmptyPageAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        KeysetPage<string, string> actual = await database.DbContext.HardDeleteEntities
            .Select(entity => entity.Name)
            .ToKeysetPageAsync(name => name, 10);

        using (Assert.EnterMultipleScope()) {
            Assert.That(actual.Items, Is.Empty);
            Assert.That(actual.NextKey, Is.Null);
            Assert.That(actual.HasMore, Is.False);
        }
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task ToKeysetPageAsync_InvalidPageSize_ThrowsArgumentOutOfRangeExceptionAsync(int pageSize) {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = database.DbContext.HardDeleteEntities
            .Select(entity => entity.Name)
            .ToKeysetPageAsync(name => name, pageSize));
    }

    private static async Task SeedAsync(TestDatabase database, params string[] names) {
        foreach (string name in names) {
            database.DbContext.HardDeleteEntities.Add(new HardDeleteEntity { Name = name });
        }

        await database.DbContext.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();
    }
}
