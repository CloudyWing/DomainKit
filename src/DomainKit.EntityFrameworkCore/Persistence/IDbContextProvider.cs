using Microsoft.EntityFrameworkCore;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Persistence;

internal interface IDbContextProvider {
    DbContext DbContext { get; }
}
