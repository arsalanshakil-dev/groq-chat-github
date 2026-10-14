# GitHub Actions deployment

Repository: https://github.com/arsalanshakil-dev/groq-chat-github

Copy `.github/workflows/ci.yml` and `scripts/deploy_ftp.py` into your repository. Replace the earlier build-only workflow with this one.

## Configure credentials

Open repository **Settings → Secrets and variables → Actions** and add these **repository secrets**:

| Secret | Value |
| --- | --- |
| `MONSTERASP_FTP_SERVER` | `site98683.siteasp.net` (confirm on the MonsterASP FTP access card) |
| `MONSTERASP_FTP_USERNAME` | Your FTP username, usually `site98683` |
| `MONSTERASP_FTP_PASSWORD` | Your FTP password |

Under **Variables**, optionally add `MONSTERASP_FTP_DIRECTORY` (default `/wwwroot/`) and `MONSTERASP_FTP_PORT` (default `21`). The directory is relative to the FTP account's view: if it already opens inside the website root, use `/` instead. Confirm the path using your FTP client before the first run.

The script uses **explicit FTPS**, not plaintext FTP or SFTP, and verifies the server certificate. Use the FTP hostname from MonsterASP's access card. Do not put credentials in the YAML or paste them into chat.

Keep `GROQ_API_KEY`, `GROQ_MODEL`, and `FRONTEND_ORIGINS=https://groq-chat-ws.netlify.app` configured on MonsterASP. The workflow does not replace these host environment settings.

## Pipeline behavior

- Pull requests to `main`: build and check both apps; no deployment.
- Push to `main`: build, run all offline checks, publish .NET, then deploy to MonsterASP.
- Actions → **Test and deploy** → **Run workflow** on `main`: manually deploy.
- Published backend and frontend files are also available as downloadable workflow artifacts.

Backend deployment briefly adds `app_offline.htm` to release IIS file locks, uploads the files, and removes the maintenance file. The 10-second pause is only for deployment; chat requests still have no retries or delays. Other remote files are preserved. FTP uploads are not atomic: if a transfer fails, inspect the server and rerun after fixing the cause. If a disconnection prevents cleanup, remove `app_offline.htm` through MonsterASP's file manager. The health check retries briefly while IIS restarts and fails if the Groq key is missing.

The job targets a GitHub environment named **production**. You can add approval rules in repository Settings → Environments if desired. Deployment jobs are serialized and are not canceled mid-upload.

## Frontend continuous deployment on Netlify

For the existing **groq-chat-ws** project, connect this GitHub repository through Netlify's project configuration for continuous deployment. Choose production branch `main`. The repository's `netlify.toml` provides base `chat-ui`, command `npm run build`, publish `dist`, and the MonsterASP HTTPS API URL.

Netlify then deploys the frontend automatically on pushes to `main`; GitHub Actions deploys the backend after checks pass. These are independent pipelines: Netlify does not wait for the GitHub check job. To require one coordinated release, use an authenticated Netlify deploy job instead of Git-connected automatic publishing.

No GitHub Pages deployment is included: GitHub hosts the source and pipeline, Netlify hosts React, and MonsterASP hosts .NET.

## First run

Add the secrets, commit both files, and push to `main`. Watch the **Test and deploy** run in Actions. Check https://groq-chat.runasp.net/api/health and send a message from https://groq-chat-ws.netlify.app after deployment.
