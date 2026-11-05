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

    private static async Task SeedAsync(TestDatabase database, params string[] names) {
        foreach (string name in names) {
            database.DbContext.HardDeleteEntities.Add(new HardDeleteEntity { Name = name });
        }

        await database.DbContext.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();
    }
}
