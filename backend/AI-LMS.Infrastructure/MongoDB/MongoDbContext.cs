using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace AI_LMS.Infrastructure.MongoDB;

public sealed class MongoDbContext
{
    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
        var databaseName = configuration["Mongo:Database"] ?? "AILMS";
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        AiQuizzes = database.GetCollection<AiQuizDocument>("ai_quizzes");
        AiSummaries = database.GetCollection<AiSummaryDocument>("ai_summaries");
        AiExplanations = database.GetCollection<AiExplanationDocument>("ai_explanations");
        AiConversations = database.GetCollection<AiConversationDocument>("ai_conversations");
        _ = EnsureIndexesAsync();
    }

    public IMongoCollection<AiQuizDocument> AiQuizzes { get; }
    public IMongoCollection<AiSummaryDocument> AiSummaries { get; }
    public IMongoCollection<AiExplanationDocument> AiExplanations { get; }
    public IMongoCollection<AiConversationDocument> AiConversations { get; }

    private async Task EnsureIndexesAsync()
    {
        try
        {
            await AiQuizzes.Indexes.CreateOneAsync(new CreateIndexModel<AiQuizDocument>(Builders<AiQuizDocument>.IndexKeys.Descending(x => x.UserId).Descending(x => x.CreatedAtUtc)));
            await AiSummaries.Indexes.CreateOneAsync(new CreateIndexModel<AiSummaryDocument>(Builders<AiSummaryDocument>.IndexKeys.Descending(x => x.UserId).Descending(x => x.CreatedAtUtc)));
            await AiExplanations.Indexes.CreateOneAsync(new CreateIndexModel<AiExplanationDocument>(Builders<AiExplanationDocument>.IndexKeys.Descending(x => x.UserId).Descending(x => x.CreatedAtUtc)));
            await AiConversations.Indexes.CreateOneAsync(new CreateIndexModel<AiConversationDocument>(Builders<AiConversationDocument>.IndexKeys.Descending(x => x.UserId).Descending(x => x.UpdatedAtUtc)));
        }
        catch
        {
            // Mongo can still connect if index creation is restricted; requests report DB failures centrally.
        }
    }
}
