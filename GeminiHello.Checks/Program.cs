using System.Net;
using System.Text.Json;

const string reply = """{"choices":[{"message":{"content":"Groq answer"}}],"usage":{"total_tokens":12}}""";
await Check("Success", "test-key", 200, reply, 0);
await Check("Missing key", null, 200, reply, 1);
await Check("Overloaded without retries", "test-key", 503, "{}", 1);
await Check("Rate limit without retries", "test-key", 429, "{}", 1);
await Check("Invalid key", "test-key", 401, "{}", 1);
await Check("Network error", "test-key", 0, "{}", 1);
await Check("Invalid JSON", "test-key", 200, "invalid", 1);
await Check("Empty response", "test-key", 200, "{\"choices\":[]}", 1);
Console.WriteLine("All 8 offline checks passed.");

async Task Check(string name, string? key, int status, string json, int expectedExit)
{
    var count = 0;
    using var http = new HttpClient(new FakeHandler(async request =>
    {
        count++;
        Require(request.RequestUri!.ToString() == "https://api.groq.com/openai/v1/chat/completions", "Groq endpoint");
        Require(request.Headers.Authorization?.ToString() == "Bearer test-key", "Authorization");
        Require(!request.Headers.Contains("x-goog-api-key"), "Unexpected authentication header");
        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
        Require(body.RootElement.GetProperty("messages")[0].GetProperty("content").GetString() == "test prompt", "Prompt");
        Require(body.RootElement.GetProperty("model").GetString() == "custom-model", "Model override");
        if (status == 0) throw new HttpRequestException("Simulated outage");
        return new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(json) };
    }));
    using var output = new StringWriter();
    using var errors = new StringWriter();
    var exit = await new AiClient(http, output, errors).RunAsync("test prompt", key, "custom-model");
    Require(exit == expectedExit, name + " exit code");
    Require(count == (key is null ? 0 : 1), name + " request count");
    if (expectedExit == 0)
        Require(output.ToString().Contains("Groq answer") && output.ToString().Contains("Tokens:"), "Answer and usage");
    else Require(errors.ToString().Length > 0, "Failure message");
    Console.WriteLine("Passed: " + name);
}
static void Require(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
sealed class FakeHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
}
