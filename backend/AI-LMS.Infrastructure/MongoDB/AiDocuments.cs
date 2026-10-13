using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AI_LMS.Infrastructure.MongoDB;

public sealed class AiQuizDocument
{
    [BsonId] public ObjectId Id { get; set; }
    public int UserId { get; set; }
    public int? CourseId { get; set; }
    public int? LessonId { get; set; }
    public string Topic { get; set; } = "";
    public string Difficulty { get; set; } = "Beginner";
    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<AiQuestionDocument> Questions { get; set; } = [];
}

public sealed class AiQuestionDocument
{
    public string Text { get; set; } = "";
    public List<string> Options { get; set; } = [];
    public int CorrectOptionIndex { get; set; }
    public decimal Marks { get; set; } = 1;
    public string Explanation { get; set; } = "";
}

public sealed class AiSummaryDocument
{
    [BsonId] public ObjectId Id { get; set; }
    public int UserId { get; set; }
    public int? CourseId { get; set; }
    public int? LessonId { get; set; }
    public string SourceText { get; set; } = "";
    public string Summary { get; set; } = "";
    public List<string> KeyPoints { get; set; } = [];
    public List<string> Concepts { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AiExplanationDocument
{
    [BsonId] public ObjectId Id { get; set; }
    public int UserId { get; set; }
    public string Question { get; set; } = "";
    public string StudentAnswer { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public string Explanation { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AiConversationDocument
{
    [BsonId] public ObjectId Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "New conversation";
    public List<AiConversationMessageDocument> Messages { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AiConversationMessageDocument
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
