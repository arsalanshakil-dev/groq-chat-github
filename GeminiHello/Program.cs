var groqKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
if (string.IsNullOrWhiteSpace(groqKey))
{
    Console.Error.WriteLine("Set GROQ_API_KEY, then run from the same terminal.");
    return 1;
}
var prompt = args.Length > 0 ? string.Join(' ', args) : "Explain tokens in two sentences.";
using var http = new HttpClient();
var model = Environment.GetEnvironmentVariable("GROQ_MODEL");
return await new AiClient(http, Console.Out, Console.Error).RunAsync(prompt, groqKey,
    string.IsNullOrWhiteSpace(model) ? "qwen/qwen3.8-27b" : model);
