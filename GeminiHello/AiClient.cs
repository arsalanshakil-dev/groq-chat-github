using System.Text.Json;

public sealed class AiClient(HttpClient http, TextWriter output, TextWriter errors)
{
    public async Task<int> RunAsync(string prompt, string? groqKey, string groqModel = "qwen/qwen3.8-27b")
    {
        if (string.IsNullOrWhiteSpace(groqKey))
        {
            errors.WriteLine("Set GROQ_API_KEY, then run from the same terminal.");
            return 1;
        }
        try
        {
            var result = await new GroqChatClient(http).SendAsync([new ChatMessage("user", prompt)], groqKey, groqModel);
            output.WriteLine("Provider: Groq");
            output.WriteLine(result.Reply);
            if (result.Usage is not null) output.WriteLine($"\nTokens: {result.Usage}");
            return 0;
        }
        catch (GroqException ex) { errors.WriteLine(ex.Message); }
        catch (HttpRequestException) { errors.WriteLine("Could not connect to Groq."); }
        catch (OperationCanceledException) { errors.WriteLine("Groq request timed out."); }
        catch (JsonException) { errors.WriteLine("Groq returned invalid JSON."); }
        return 1;
    }
}
