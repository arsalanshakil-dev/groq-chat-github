using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

public record ChatMessage(string Role, string Content);
public record ChatReply(string Reply, JsonElement? Usage);
public sealed class GroqException(string message, int statusCode = 502) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class GroqChatClient(HttpClient http)
{
    public async Task<ChatReply> SendAsync(IReadOnlyList<ChatMessage> messages, string key,
        string model, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,
            "https://api.groq.com/openai/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Content = JsonContent.Create(new
        {
            model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
            max_completion_tokens = 1000
        });
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new GroqException($"Groq returned {(int)response.StatusCode}. " +
                ((int)response.StatusCode == 429 ? "Rate limit reached. Please try again later." :
                 (int)response.StatusCode >= 500 ? "The service is unavailable. Please try again later." :
                 "Check the server API key, model, and permissions."), (int)response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;
        if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0 || !choices[0].TryGetProperty("message", out var message)
            || !message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(content.GetString()))
            throw new GroqException("Groq returned no text. Try a different prompt.");
        return new ChatReply(content.GetString()!, root.TryGetProperty("usage", out var usage) ? usage.Clone() : null);
    }
}
