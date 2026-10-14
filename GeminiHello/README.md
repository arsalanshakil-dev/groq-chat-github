# Groq Console App

A small .NET 10 console app using HttpClient to call Groq. No SDK or NuGet packages are needed. It reads GROQ_API_KEY from the environment, prints the answer and token usage, and makes one request with no retries or added delays.

## Run in PowerShell

Create a key at https://console.groq.com/keys, then run in the same terminal:

```powershell
cd GeminiHello
$env:GROQ_API_KEY = "paste-your-groq-key-here"
dotnet run
dotnet run -- "Give me three tips for learning C#"
```

Keep your key out of source files and chat. The app sends it only as a Bearer authorization header to api.groq.com.

The default prompt is "Explain tokens in two sentences." The default model is qwen/qwen3.8-27b, with a limit of 1000 completion tokens. To choose another model, set `$env:GROQ_MODEL = "your-model-id"`. Check https://console.groq.com/docs/models for available models.

Errors are printed and return exit code 1. For 401/403, check the key and permissions. For 404, check the model. For 429, check quota. For 503, try again later. Empty replies, invalid JSON, connection errors, and timeouts also return exit code 1.

## Offline checks

```powershell
dotnet run --project ../GeminiHello.Checks
```

Checks use simulated responses without API keys or network calls, covering success, missing keys, request authentication, model overrides, and failures without retries.
