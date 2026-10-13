using System.Text;
using System.Text.Json;
using AI_LMS.Application.DTOs;
using AI_LMS.Application.Exceptions;
using AI_LMS.Application.Interfaces;
using AI_LMS.Infrastructure.Data;
using AI_LMS.Infrastructure.MongoDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AI_LMS.Infrastructure.Services;

public sealed class AIService(
    HttpClient httpClient,
    IConfiguration configuration,
    MongoDbContext mongo,
    ILearningService learning,
    ApplicationDbContext db) : IAIService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ============================================================
    // AI QUIZ GENERATION
    // ============================================================

    public async Task<GenerateQuizResponse> GenerateQuizAsync(
        GenerateQuizRequest request,
        int userId,
        CancellationToken cancellationToken = default)
    {

        var prompt =
            $"Create exactly {request.Count} multiple choice questions about '{request.Topic}' " +
            $"at {request.Difficulty} level. Return JSON only: " +
            """{"questions":[{"text":"...","options":["A","B","C","D"],"correctOptionIndex":0,"marks":1,"explanation":"..."}]}""";

        var raw = await AskOllamaAsync(
            "You are an instructional designer. Return only valid JSON matching the requested schema.",
            prompt,
            cancellationToken);


        var questions = ParseQuiz(raw, request.Count);
        var usedAi = questions is not null;

        questions ??= FallbackQuiz(
            request.Topic,
            request.Count,
            request.Difficulty);

        var doc = new AiQuizDocument
        {
            Id = ObjectId.GenerateNewId(),
            UserId = userId,
            CourseId = request.CourseId,
            LessonId = request.LessonId,
            Topic = request.Topic.Trim(),
            Difficulty = request.Difficulty,
            IsPublished = false,
            Questions = questions.Select(q => new AiQuestionDocument
            {
                Text = q.Text,
                Options = q.Options.ToList(),
                CorrectOptionIndex = q.CorrectOptionIndex,
                Marks = q.Marks,
                Explanation = q.Explanation
            }).ToList()
        };

        await mongo.AiQuizzes.InsertOneAsync(
            doc,
            cancellationToken: cancellationToken);

        return new GenerateQuizResponse(
            doc.Id.ToString(),
            usedAi,
            questions);
    }

    // ============================================================
    // PUBLISH AI QUIZ
    // ============================================================

    public async Task<QuizDto> PublishQuizAsync(
        string documentId,
        int userId,
        QuizRequest editedQuiz,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(documentId, out var id))
            throw new LmsException(
                400,
                "Invalid AI quiz document id.");

        var filter =
            Builders<AiQuizDocument>.Filter.Eq(x => x.Id, id) &
            Builders<AiQuizDocument>.Filter.Eq(x => x.UserId, userId);

        var draft = await mongo.AiQuizzes
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new LmsException(
                404,
                "AI quiz draft not found.");

        if (draft.IsPublished)
            throw new LmsException(
                409,
                "This AI quiz draft has already been published.");

        if (!draft.CourseId.HasValue)
            throw new LmsException(
                400,
                "Select a course before publishing the generated quiz.");

        var quizRequest = new QuizRequest
        {
            Title = editedQuiz.Title,
            Description = editedQuiz.Description,
            TimeLimitMinutes = editedQuiz.TimeLimitMinutes,
            PassingMarks = editedQuiz.PassingMarks,
            Publish = editedQuiz.Publish,
            IsAiGenerated = true,
            Questions = editedQuiz.Questions,
            AssignedStudentIds = editedQuiz.AssignedStudentIds
        };

        var published = await learning.CreateQuizAsync(
            draft.CourseId.Value,
            quizRequest,
            cancellationToken);

        await mongo.AiQuizzes.UpdateOneAsync(
            filter,
            Builders<AiQuizDocument>.Update
                .Set(x => x.IsPublished, true),
            cancellationToken: cancellationToken);

        return published;
    }

    // ============================================================
    // AI SUMMARY
    // ============================================================

    public async Task<SummaryResponse> GenerateSummaryAsync(
        TextRequest request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var prompt =
            "Summarise this training material. Return JSON only: " +
            "{\"summary\":\"2-4 concise sentences\"," +
            "\"keyPoints\":[\"...\"],\"concepts\":[\"...\"]}." +
            "\n\nMaterial:\n" +
            request.Content;

        var raw = await AskOllamaAsync(
            "You are a friendly course tutor. Make accurate, concise study notes.",
            prompt,

            cancellationToken);

        SummaryPayload? parsed = null;

        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                parsed = JsonSerializer.Deserialize<SummaryPayload>(
                    ExtractJson(raw),
                    JsonOptions);
            }
            catch (JsonException)
            {
            }
        }

        var usedAi = parsed is { Summary.Length: > 0 };

        var summary = usedAi
            ? parsed!.Summary
            : MakeSummary(request.Content);

        var points = usedAi && parsed!.KeyPoints?.Count > 0
            ? parsed.KeyPoints.Take(8).ToList()
            : ExtractSentences(request.Content)
                .Take(6)
                .ToList();

        var concepts = usedAi && parsed!.Concepts?.Count > 0
            ? parsed.Concepts.Take(8).ToList()
            : ExtractConcepts(request.Content)
                .Take(6)
                .ToList();

        var document = new AiSummaryDocument
        {
            UserId = userId,
            CourseId = request.CourseId,
            LessonId = request.LessonId,
            SourceText = request.Content[
                ..Math.Min(request.Content.Length, 12000)],
            Summary = summary,
            KeyPoints = points,
            Concepts = concepts
        };

        await mongo.AiSummaries.InsertOneAsync(
            document,
            cancellationToken: cancellationToken);

        return new SummaryResponse(
            document.Id.ToString(),
            usedAi,
            summary,
            points,
            concepts);
    }

    // ============================================================
    // AI EXPLANATION
    // ============================================================

    public async Task<ExplainResponse> ExplainAsync(
        ExplainRequest request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var prompt =
            "Explain this multiple-choice answer to a beginner. " +
            $"Question: {request.Question}\n" +
            $"Student answered: {request.StudentAnswer}\n" +
            $"Correct answer: {request.CorrectAnswer}\n" +
            "Clearly explain why the student's answer is wrong and why " +
            "the correct answer is right. Use a short example.";

        var raw = await AskOllamaAsync(
            "You are a patient beginner-friendly tutor. Be accurate, supportive, and concise.",
            prompt,
            cancellationToken);

        var usedAi = !string.IsNullOrWhiteSpace(raw);

        var explanation = usedAi
            ? raw!.Trim()
            : $"The correct answer is ‘{request.CorrectAnswer}’. " +
              $"Your answer ‘{request.StudentAnswer}’ does not match the key idea " +
              "in the question. Review the related lesson, compare each option " +
              "with the question requirements, and try a small example. " +
              "Understanding why an option fits—not just memorising its wording—" +
              "will help next time.";

        var doc = new AiExplanationDocument
        {
            UserId = userId,
            Question = request.Question,
            StudentAnswer = request.StudentAnswer,
            CorrectAnswer = request.CorrectAnswer,
            Explanation = explanation
        };

        await mongo.AiExplanations.InsertOneAsync(
            doc,
            cancellationToken: cancellationToken);

        return new ExplainResponse(
            doc.Id.ToString(),
            usedAi,
            explanation);
    }

    // ============================================================
    // ROLE-BASED AI CHAT
    // ============================================================

    public async Task<ChatResponse> ChatAsync(
        ChatRequest request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(
                x => x.Id == userId,
                cancellationToken);

        if (user is null)
            throw new LmsException(
                404,
                "User account not found.");

        var role = user.Role?.Name?.Trim() ?? "Student";
        var rolePrompt = GetRolePrompt(role);

        AiConversationDocument conversation;

        if (string.IsNullOrWhiteSpace(request.ConversationId))
        {
            var title = request.Message.Trim();

            conversation = new AiConversationDocument
            {
                Id = ObjectId.GenerateNewId(),
                UserId = userId,
                Title = title[..Math.Min(title.Length, 72)]
            };

            await mongo.AiConversations.InsertOneAsync(
                conversation,
                cancellationToken: cancellationToken);
        }
        else
        {
            if (!ObjectId.TryParse(
                    request.ConversationId,
                    out var id))
            {
                throw new LmsException(
                    400,
                    "Invalid conversation id.");
            }

            conversation = await mongo.AiConversations
                .Find(x =>
                    x.Id == id &&
                    x.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new LmsException(
                    404,
                    "Conversation not found.");
        }

        var recent = conversation.Messages
            .TakeLast(12)
            .Select(m =>
                (m.Role == "assistant"
                    ? "AI Assistant: "
                    : "User: ") +
                m.Content)
            .ToList();

        recent.Add("User: " + request.Message.Trim());

        // Keep chat prompts and responses short to reduce generation time.
        var prompt =
            string.Join("\n", recent) +
            "\nAnswer the latest question directly." +
            "\nUse simple English and a maximum of 40 - 60 words." +
            "\nGive one short example when useful." +
            "\nAvoid long introductions and repetition.";

        var raw = await AskOllamaAsync(
            rolePrompt,
            prompt,
            cancellationToken,
            numPredict: 80);

        var usedAi = !string.IsNullOrWhiteSpace(raw);

        var answer = usedAi
            ? raw!.Trim()
            : OfflineRoleBasedAnswer(
                role,
                request.Message);

        var now = DateTime.UtcNow;

        conversation.Messages.Add(
            new AiConversationMessageDocument
            {
                Role = "user",
                Content = request.Message.Trim(),
                CreatedAtUtc = now
            });

        conversation.Messages.Add(
            new AiConversationMessageDocument
            {
                Role = "assistant",
                Content = answer,
                CreatedAtUtc = now
            });

        conversation.UpdatedAtUtc = now;

        var conversationFilter =
            Builders<AiConversationDocument>.Filter.Eq(
                x => x.Id,
                conversation.Id) &
            Builders<AiConversationDocument>.Filter.Eq(
                x => x.UserId,
                userId);

        await mongo.AiConversations.ReplaceOneAsync(
            conversationFilter,
            conversation,
            cancellationToken: cancellationToken);

        return new ChatResponse(
            conversation.Id.ToString(),
            usedAi,
            answer);
    }

    // ============================================================
    // ROLE PROMPTS
    // ============================================================

    private static string GetRolePrompt(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "trainer" =>
                """
                You are the AI Teaching Assistant for an AI-powered Learning Management System.
                The current user is a TRAINER.

                Help with teaching, course development, lesson ideas, examples,
                exercises, assignments, quizzes, study notes and course feedback.

                Be professional, practical and concise.
                Use examples when useful.
                Do not claim access to LMS data unless it is provided.
                Do not invent student performance statistics.
                """,

            "admin" =>
                """
                You are the AI LMS Assistant for an AI-powered Learning Management System.
                The current user is an ADMINISTRATOR.

                Help with LMS administration, course management, user management,
                training programs, student support, trainer support, reports and
                course quality.

                Be professional, concise and practical.
                Distinguish suggestions from actual LMS data.
                Do not invent users, courses, marks or statistics.
                Ask for specific data when required.
                """,

            _ =>
                """
                You are the AI Study Assistant for an AI-powered Learning Management System.
                The current user is a STUDENT.

                Help explain course topics, simplify difficult concepts, give examples,
                summarise learning material, create study plans, explain mistakes,
                provide practice questions and suggest what to study next.

                Use beginner-friendly language.
                Be supportive and concise.
                Prefer practical examples.
                Help students understand rather than simply giving answers to graded assessments.
                """
        };
    }

    // ============================================================
    // OFFLINE FALLBACK
    // ============================================================

    private static string OfflineRoleBasedAnswer(
        string role,
        string question)
    {
        return role.ToLowerInvariant() switch
        {
            "trainer" =>
                $"Let's approach this as a teaching task.\n\n" +
                $"Your request: {question.Trim()}\n\n" +
                "I can help you turn this into a lesson, example, exercise, quiz or assignment. " +
                "Make sure Ollama is running to get a fully AI-generated response.",

            "admin" =>
                $"Let's approach this from an LMS management perspective.\n\n" +
                $"Your request: {question.Trim()}\n\n" +
                "I can help structure the analysis and suggest improvements. " +
                "Make sure Ollama is running to get a fully AI-generated response.",

            _ =>
                $"Let's break this down step by step.\n\n" +
                $"Your question: {question.Trim()}\n\n" +
                "Start with a simple example and connect one new idea at a time. " +
                "Make sure Ollama is running to get a fully AI-generated response."
        };
    }

    // ============================================================
    // CONVERSATIONS
    // ============================================================

    public async Task<IReadOnlyList<ConversationDto>> ListConversationsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var docs = await mongo.AiConversations
            .Find(x => x.UserId == userId)
            .SortByDescending(x => x.UpdatedAtUtc)
            .Limit(100)
            .ToListAsync(cancellationToken);

        return docs.Select(MapConversation).ToList();
    }

    public async Task<ConversationDto?> GetConversationAsync(
        string id,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        var doc = await mongo.AiConversations
            .Find(x =>
                x.Id == objectId &&
                x.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        return doc is null
            ? null
            : MapConversation(doc);
    }

    public async Task<bool> DeleteConversationAsync(
        string id,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            throw new LmsException(
                400,
                "Invalid conversation id.");

        var result = await mongo.AiConversations.DeleteOneAsync(
            x =>
                x.Id == objectId &&
                x.UserId == userId,
            cancellationToken);

        return result.DeletedCount > 0;
    }

    // ============================================================
    // OLLAMA LOCAL AI REQUEST
    // ============================================================

    private async Task<string?> AskOllamaAsync(
        string system,
        string prompt,
        CancellationToken cancellationToken,
        int numPredict = 350)
    {
        var baseUrl =
            configuration["Ollama:BaseUrl"]
            ?? "http://localhost:11434";

        var model =
            configuration["Ollama:Model"]
            ?? "llama3.2:3b";

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("OLLAMA: Local AI");
        Console.WriteLine($"OLLAMA URL: {baseUrl}");
        Console.WriteLine($"OLLAMA MODEL: {model}");
        Console.WriteLine("OLLAMA: Sending request...");
        Console.WriteLine("========================================");
        Console.WriteLine();

        try
        {
            var requestBody = new
            {
                model = model,
                stream = false,

                // Keep Ollama's model loaded for 30 minutes.
                keep_alive = "30m",

                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = system
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },

                options = new
                {
                    temperature = 0.4,
                    num_predict = numPredict
                }
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{baseUrl.TrimEnd('/')}/api/chat");

            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            using var response = await httpClient.SendAsync(
                request,
                cancellationToken);

            var responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine(
                $"OLLAMA STATUS: {(int)response.StatusCode} {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("OLLAMA ERROR RESPONSE:");
                Console.WriteLine(responseBody);
                Console.WriteLine("========================================");
                Console.WriteLine();

                return null;
            }

            Console.WriteLine("OLLAMA: Request successful");
            Console.WriteLine("========================================");
            Console.WriteLine();

            using var json = JsonDocument.Parse(responseBody);

            if (!json.RootElement.TryGetProperty(
                    "message",
                    out var message))
            {
                Console.WriteLine("OLLAMA ERROR: MESSAGE PROPERTY NOT FOUND");
                return null;
            }

            if (!message.TryGetProperty(
                    "content",
                    out var contentElement))
            {
                Console.WriteLine("OLLAMA ERROR: CONTENT PROPERTY NOT FOUND");
                return null;
            }

            var content = contentElement.GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                Console.WriteLine("OLLAMA ERROR: EMPTY RESPONSE");
                return null;
            }

            return content.Trim();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("OLLAMA REQUEST FAILED");
            Console.WriteLine("HTTP ERROR:");
            Console.WriteLine(ex.Message);
            Console.WriteLine();
            Console.WriteLine("Make sure Ollama is installed and running.");
            Console.WriteLine("Expected URL: http://localhost:11434");
            Console.WriteLine("========================================");
            Console.WriteLine();

            return null;
        }
        catch (JsonException ex)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("OLLAMA RESPONSE JSON ERROR");
            Console.WriteLine(ex.Message);
            Console.WriteLine("========================================");
            Console.WriteLine();

            return null;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("OLLAMA REQUEST TIMEOUT/CANCELLED");
            Console.WriteLine(ex.Message);
            Console.WriteLine("========================================");
            Console.WriteLine();

            return null;
        }
    }

    // ============================================================
    // QUIZ JSON PARSER
    // ============================================================

    private static List<GeneratedQuestionDto>? ParseQuiz(
        string? raw,
        int count)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        try
        {
            var parsed =
                JsonSerializer.Deserialize<QuizPayload>(
                    ExtractJson(raw),
                    JsonOptions);

            if (parsed?.Questions is null ||
                parsed.Questions.Count < count)
            {
                return null;
            }

            var questions = parsed.Questions.Take(count).ToList();

            if (questions.Any(q =>
                string.IsNullOrWhiteSpace(q.Text) ||
                q.Options is null ||
                q.Options.Count is < 2 or > 6 ||
                q.CorrectOptionIndex < 0 ||
                q.CorrectOptionIndex >= q.Options.Count))
            {
                return null;
            }

            return questions.Select(q =>
                new GeneratedQuestionDto(
                    q.Text.Trim(),
                    q.Options!
                        .Select(x => x.Trim())
                        .ToList(),
                    q.CorrectOptionIndex,
                    Math.Max(1, q.Marks),
                    q.Explanation ??
                    "Review the lesson to understand this answer."
                )).ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // ============================================================
    // EXTRACT JSON
    // ============================================================

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');

        return start >= 0 && end > start
            ? raw[start..(end + 1)]
            : raw;
    }

    // ============================================================
    // FALLBACK QUIZ
    // ============================================================

    private static List<GeneratedQuestionDto> FallbackQuiz(
        string topic,
        int count,
        string difficulty)
    {
        var data =
            new (string Question, string Correct, string Explanation)[]
            {
                (
                    $"Which statement best describes {topic}?",
                    $"{topic} is a core concept used to solve problems in this subject",
                    $"This describes the core purpose of {topic}."
                ),
                (
                    $"What is a useful first step when learning {topic}?",
                    $"Understand the fundamentals and practise a small example of {topic}",
                    $"Small practical examples help you build a correct mental model of {topic}."
                ),
                (
                    $"Which approach is a best practice for {topic}?",
                    "Follow standard conventions, validate your work, and document assumptions",
                    "Reliable practice includes conventions, validation and clear documentation."
                ),
                (
                    $"Why do developers use {topic}?",
                    "To structure work and build maintainable solutions",
                    "Maintainability and clear structure are common reasons to use this concept."
                ),
                (
                    $"How should you check your understanding of {topic}?",
                    "Explain the idea in your own words and test a small example",
                    "An explanation and a working example are good learning checks."
                )
            };

        return Enumerable.Range(0, count).Select(i =>
        {
            var d = data[i % data.Length];

            return new GeneratedQuestionDto(
                $"[{difficulty}] {d.Question}",
                [
                    d.Correct,
                    "Ignore the requirements and guess randomly",
                    "Skip validation and testing",
                    "Avoid learning the underlying concept"
                ],
                0,
                1,
                d.Explanation);
        }).ToList();
    }

    // ============================================================
    // SUMMARY FALLBACK
    // ============================================================

    private static string MakeSummary(string content)
    {
        var sentences = ExtractSentences(content);

        return sentences.Count == 0
            ? "Add lesson text to create a summary."
            : string.Join(" ", sentences.Take(3));
    }

    // ============================================================
    // EXTRACT SENTENCES
    // ============================================================

    private static List<string> ExtractSentences(string content) =>
        content
            .Replace('\n', ' ')
            .Split(
                ['.', '!', '?'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 18)
            .Take(8)
            .Select(s =>
                s.EndsWith('.')
                    ? s
                    : s + ".")
            .ToList();

    // ============================================================
    // EXTRACT CONCEPTS
    // ============================================================

    private static List<string> ExtractConcepts(string content) =>
        content
            .Split(
                [' ', '\n', ',', '.', ':', ';', '(', ')'],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(w =>
                w.Length > 6 &&
                char.IsLetter(w[0]))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

    // ============================================================
    // MAP CONVERSATION
    // ============================================================

    private static ConversationDto MapConversation(
        AiConversationDocument d) =>
        new(
            d.Id.ToString(),
            d.Title,
            d.CreatedAtUtc,
            d.Messages.Select(m =>
                new ConversationMessageDto(
                    m.Role,
                    m.Content,
                    m.CreatedAtUtc))
            .ToList());

    // ============================================================
    // JSON PAYLOAD CLASSES
    // ============================================================

    private sealed class QuizPayload
    {
        public List<QuestionPayload>? Questions { get; set; }
    }

    private sealed class QuestionPayload
    {
        public string Text { get; set; } = "";

        public List<string>? Options { get; set; }

        public int CorrectOptionIndex { get; set; }

        public decimal Marks { get; set; } = 1;

        public string? Explanation { get; set; }
    }

    private sealed class SummaryPayload
    {
        public string Summary { get; set; } = "";

        public List<string>? KeyPoints { get; set; }

        public List<string>? Concepts { get; set; }
    }
}