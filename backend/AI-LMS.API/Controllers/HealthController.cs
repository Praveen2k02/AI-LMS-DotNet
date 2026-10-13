using AI_LMS.API.Extensions;
using AI_LMS.Infrastructure.Data;
using AI_LMS.Infrastructure.MongoDB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace AI_LMS.API.Controllers;

[Route("api/health")]
public sealed class HealthController(ApplicationDbContext db, MongoDbContext mongo) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
        await mongo.AiConversations.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
        return Ok(new { success = true, status = "healthy", sqlServer = "connected", mongoDb = "connected" });
    }
}
