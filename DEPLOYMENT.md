# MonsterASP backend + Netlify frontend

The source is configured for a backend at `https://groq-chat.runasp.net`. Confirm HTTPS works before publishing the frontend; browsers block HTTP API calls from an HTTPS Netlify site.

## 1. Backend

Generate the backend files in `artifacts/monsterasp` from the repository root:

```powershell
dotnet publish ChatApi/ChatApi.csproj -p:PublishProfile=MonsterAsp
```

In MonsterASP, open the website for `groq-chat.runasp.net`:

1. Choose ASP.NET Core / .NET 10 for the website.
2. Upload the **contents** of `artifacts/monsterasp` to the website application root using the file manager/FTP, or use the ZIP deployment option. `web.config` and `ChatApi.dll` must be in the application root, not an extra nested folder. For an existing running app, stop it or use the host's app-offline option before replacing files.
3. Under **Scripting → Environment variables**, add:

| Name | Value |
| --- | --- |
| `GROQ_API_KEY` | Your Groq key; enter it directly in MonsterASP |
| `GROQ_MODEL` | Optional model available to your account; default is `qwen/qwen3.8-27b` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `FRONTEND_ORIGINS` | Your exact Netlify HTTPS origin, e.g. `https://your-site.netlify.app` (no trailing slash) |

4. Enable HTTPS for the hostname using the control panel if needed.
5. Restart the application. Open `https://groq-chat.runasp.net/api/health`; expect `{"configured":true}`.

You can set multiple allowed frontend origins separated by commas. CORS enables the browser connection; it does not authenticate users or prevent direct API calls. This demo has no login or abuse protection, so keep Groq usage limits in mind when sharing the public site.

## 2. Netlify

### Upload the built UI (quickest)

Build the frontend using the commands below, then use the Netlify dashboard manual deploy/drop area to upload the **chat-ui/dist folder**. The backend URL is `https://groq-chat.runasp.net`.

After Netlify gives you the site URL, set that exact origin as `FRONTEND_ORIGINS` in MonsterASP and restart the backend. Never put GROQ_API_KEY into Netlify frontend environment variables.

### Build from a Git repository

Push the source to your repository and import it in Netlify. The root `netlify.toml` sets:

- Base: `chat-ui`
- Build command: `npm run build`
- Publish directory: `dist`
- Public API setting: `VITE_API_BASE_URL=https://groq-chat.runasp.net`

The API URL is public and safe to bundle. To change it, update `netlify.toml` and redeploy; its value takes precedence over a matching dashboard variable.

### Rebuild for manual upload

```powershell
cd chat-ui
npm ci
$env:VITE_API_BASE_URL = "https://groq-chat.runasp.net"
npm run build
```

## 3. Verify

Open the Netlify site and send a message. Test Send chat history on and off. If the browser reports a network/CORS error, confirm the backend uses HTTPS, FRONTEND_ORIGINS matches the frontend URL exactly, and the backend was restarted. For a missing-key message, set GROQ_API_KEY on MonsterASP and restart. For 429/503, check Groq quota/service availability; there are no automatic retries.

## Local single-server preview

Use `npm run build:dotnet` in `chat-ui` to put the UI into `ChatApi/wwwroot`. Run the API at localhost:5080 as described in README.md. Standard `npm run build` now writes `chat-ui/dist` for Netlify.
