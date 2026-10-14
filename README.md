# Groq Chat

A React chat UI with a .NET 10 API. Both the original console app and API share the Groq client. API keys stay on the server. Requests include conversation history and use no retries or added delays.

## Start the chat

Install .NET 10 and Node.js 20.19 or newer. From PowerShell:

```powershell
cd chat-ui
npm ci
npm run build:dotnet
cd ..
$env:GROQ_API_KEY = "paste-your-groq-key-here"
# Optional: choose a model available to your Groq account
# $env:GROQ_MODEL = "your-model-id"
dotnet run --project ChatApi --urls http://localhost:5080
```

Open http://localhost:5080. The default model is qwen/qwen3.8-27b. Messages are held in browser memory; refreshing or selecting New chat clears them. A conversation is limited to 40 messages and 60,000 total characters.

Use **Send chat history** to compare responses with and without conversation context. It starts on. When off, only the latest user message is sent, while the full conversation remains visible. Each reply indicates how many messages were sent for that request. Switching history on again includes the visible conversation in the next request. The request limits apply to the messages actually sent.

For a demonstration, tell the model your name, then ask for it with history on and off. Expand **Try a memory experiment** for an explanation. Run `npm test` in `chat-ui` to verify that no-history requests exclude all earlier turns.

## Frontend development

Start the API as above. In a second terminal:

```powershell
cd chat-ui
npm run dev
```

Open http://localhost:5173. Vite forwards `/api` requests to the .NET server on port 5080. `npm run build:dotnet` puts the finished UI into `ChatApi/wwwroot`, served by .NET. `npm run build` writes `chat-ui/dist` for Netlify. See DEPLOYMENT.md for MonsterASP + Netlify setup.

## API

`POST /api/chat` accepts `{"messages":[{"role":"user","content":"Hello"}]}` and returns `{"reply":"...","usage":{...}}`. Send user and assistant history with each request, ending in a user message. Failures return `{"error":"..."}`. `GET /api/health` reports whether the server key is configured, without returning the key.

The server binds to localhost in these instructions. The app has no user accounts or persistent storage.

## Console and checks

```powershell
dotnet run --project GeminiHello -- "Explain tokens in two sentences."
dotnet run --project GeminiHello.Checks
```

The existing console folder name remains for compatibility; all API calls use Groq.

## Repository structure

- `ChatApi/`: ASP.NET Core API for the React frontend.
- `chat-ui/`: React + Vite frontend and context-mode tests.
- `GeminiHello/`: optional Groq console app and the shared client (historical folder name).
- `GeminiHello.Checks/`: offline checks using simulated Groq responses.
- `GroqChat.slnx`: .NET solution.
- `netlify.toml`: frontend deployment settings.
- `.github/workflows/ci.yml`: builds both apps and runs offline checks on pushes and pull requests; no API key is required.

## Upload to GitHub

Extract the source ZIP and upload its **contents** to your repository, including `.github`, `.gitignore`, and `.gitattributes`. Do not upload the ZIP itself as the project source. Alternatively, run these commands from the extracted folder:

```powershell
git init
git add .
git commit -m "Add Groq chat app"
git branch -M main
git remote add origin https://github.com/YOUR-USERNAME/YOUR-REPOSITORY.git
git push -u origin main
```

These commands assume a new, empty GitHub repository. For an existing repository, copy the source into its checkout, review the changes, then commit and push using its existing remote.

API keys belong only in the backend host environment. `.env` files, deployment credentials, build output, dependencies, and local workshop documents are excluded. The credential-free `MonsterAsp.pubxml` folder publish profile is safe to commit; downloaded `.publishSettings` files are not.

## Deploy from GitHub

Import the repository into Netlify; `netlify.toml` supplies the frontend build settings and current backend URL. See [DEPLOYMENT.md](DEPLOYMENT.md) to publish the .NET API to MonsterASP and configure the Netlify origin there. CI verifies source changes; it does not upload the backend automatically.
