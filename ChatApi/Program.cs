var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<GroqChatClient>();
var frontendOrigins = (builder.Configuration["FRONTEND_ORIGINS"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(frontendOrigins).WithMethods("GET", "POST").AllowAnyHeader()));
var app = builder.Build();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/health", () => Results.Ok(new { configured = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GROQ_API_KEY")) }));
app.MapPost("/api/chat", async (ChatRequest request, GroqChatClient client, CancellationToken cancellationToken) =>
{
    if (request.Messages is null || request.Messages.Count is < 1 or > 40
        || request.Messages.Any(m => m is null || m.Role is not ("user" or "assistant")
            || string.IsNullOrWhiteSpace(m.Content) || m.Content.Length > 12000)
        || request.Messages.Sum(m => m.Content.Length) > 60000
        || request.Messages[^1].Role != "user")
        return Results.BadRequest(new { error = "Send 1–40 messages, ending with a user message (maximum 12,000 characters per message)." });
    var key = Environment.GetEnvironmentVariable("GROQ_API_KEY");
    if (string.IsNullOrWhiteSpace(key))
        return Results.Json(new { error = "Set GROQ_API_KEY on the server, then restart it." }, statusCode: 503);
    var model = Environment.GetEnvironmentVariable("GROQ_MODEL");
    try
    {
        var reply = await client.SendAsync(request.Messages, key,
            string.IsNullOrWhiteSpace(model) ? "qwen/qwen3.8-27b" : model, cancellationToken);
        return Results.Ok(reply);
    }
    catch (GroqException ex) { return Results.Json(new { error = ex.Message }, statusCode: ex.StatusCode); }
    catch (HttpRequestException) { return Results.Json(new { error = "Could not connect to Groq." }, statusCode: 502); }
    catch (OperationCanceledException) { return Results.Json(new { error = "The request was canceled or timed out." }, statusCode: 504); }
    catch (System.Text.Json.JsonException) { return Results.Json(new { error = "Groq returned an unreadable response." }, statusCode: 502); }
});
app.Run();
record ChatRequest(List<ChatMessage>? Messages);
