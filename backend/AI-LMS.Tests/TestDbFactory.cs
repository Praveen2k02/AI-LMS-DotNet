using AI_LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AI_LMS.Tests;

internal static class TestDbFactory
{
    public static ApplicationDbContext Create() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"AILMS-tests-{Guid.NewGuid():N}")
        .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
        .Options);
}
