# 🚀 Deployment Guide — AI Trading System

> **Backend API** → Render.com (Free Docker web service)  
> **Frontend WASM** → Netlify (Free static hosting)

---

## Prerequisites

- GitHub account (free)
- Render.com account (free — sign up with GitHub)
- Netlify account (free — sign up with GitHub)
- Your code pushed to a GitHub repository

---

## Step 1: Push Code to GitHub

```bash
cd D:\Projects\Test\TredingSystem
git init
git add .
git commit -m "Initial commit - AI Trading System"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/TredingSystem.git
git push -u origin main
```

---

## Step 2: Deploy Backend API to Render.com

### 2.1 Create a Render account
1. Go to [https://render.com](https://render.com)
2. Sign up with your **GitHub account** (free)

### 2.2 Create a new Web Service
1. Click **"New +"** → **"Web Service"**
2. Connect your GitHub repo: `YOUR_USERNAME/TredingSystem`
3. Configure:

| Setting | Value |
|---------|-------|
| **Name** | `tradingsystem-api` |
| **Region** | Oregon (US West) or nearest |
| **Runtime** | **Docker** |
| **Dockerfile Path** | `./Dockerfile.api` |
| **Docker Context** | `.` |
| **Instance Type** | **Free** |

4. Add **Environment Variables**:

| Key | Value |
|-----|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:10000` |

5. Click **"Create Web Service"**

### 2.3 Wait for deployment
- Render will build your Docker image and deploy (~3-5 minutes)
- Once done, you'll get a URL like: `https://tradingsystem-api.onrender.com`
- Test it: visit `https://tradingsystem-api.onrender.com/swagger`

### 2.4 Update CORS with your frontend URL
After Step 3, edit `src/TradingSystem.Api/appsettings.Production.json`:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://YOUR_USERNAME.github.io",
      "https://localhost:5002"
    ]
  }
}
```
Commit & push — Render will auto-redeploy.

---

## Step 3: Deploy Frontend to Netlify

### 3.1 Create a Netlify account
1. Go to [https://netlify.com](https://netlify.com)
2. Sign up with your **GitHub account** (free)

### 3.2 Update API URL before deploying
Edit `src/TradingSystem.Web/wwwroot/appsettings.json` with your actual Render URL:
```json
{
  "ApiBaseUrl": "https://tradingsystem-api.onrender.com"
}
```
Commit and push this change.

### 3.3 Create a new site on Netlify
1. Click **"Add new site"** → **"Import an existing project"**
2. Select **GitHub** → choose your `TredingSystem` repo
3. Configure build settings:

| Setting | Value |
|---------|-------|
| **Branch to deploy** | `main` |
| **Build command** | `dotnet publish src/TradingSystem.Web/TradingSystem.Web.csproj -c Release -o output` |
| **Publish directory** | `output/wwwroot` |

4. Click **"Deploy site"**

### 3.4 Set a custom site name (optional)
1. Go to **Site configuration** → **Site details** → **Change site name**
2. Choose something like `tradingsystem` → your URL becomes `https://tradingsystem.netlify.app`

### 3.5 Update backend CORS
After you know your Netlify URL, update `src/TradingSystem.Api/appsettings.Production.json`:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://YOUR_APP_NAME.netlify.app",
      "https://localhost:5002"
    ]
  }
}
```
Commit & push — Render will auto-redeploy with the updated CORS.

---

## Step 4: Verify Everything Works

1. ✅ **API Swagger**: `https://tradingsystem-api.onrender.com/swagger`
2. ✅ **Frontend**: `https://YOUR_APP_NAME.netlify.app`
3. ✅ **Login**: Use `admin` / `Admin@123`
4. ✅ **Live Data**: Search stocks (AAPL, MSFT, RELIANCE) — data from Yahoo Finance
5. ✅ **Markets**: Check NSE/BSE/US/Global tabs

---

## ⚠️ Important Notes

### Render Free Tier
- **Spins down after 15 minutes of inactivity** — first request after idle takes ~30-60s
- 750 free hours/month
- **Tip**: Use [UptimeRobot](https://uptimerobot.com) (free) to ping your API every 14 minutes

### Netlify Free Tier
- **300 build minutes/month** — more than enough
- **100 GB bandwidth/month**
- Automatic HTTPS, global CDN
- Auto-deploys on every push to `main`
- SPA routing handled via `_redirects` file (already configured)

### SignalR on Free Tier
- WebSocket connections may be limited on Render free tier
- App still works — real-time updates may fall back to long polling

---

## Quick Troubleshooting

| Issue | Fix |
|-------|-----|
| CORS errors in browser | Update `appsettings.Production.json` CORS with your exact Netlify URL, push |
| API returns 404 | Check Render logs — ensure Docker builds correctly |
| Blank page on Netlify | Check browser console — likely API URL mismatch in `appsettings.json` |
| Login fails | Ensure API is running and CORS configured |
| Slow first load | Render free tier cold start — wait 30s, refresh |
| Netlify build fails | Check build logs in Netlify dashboard |

---

## File Summary

| File | Purpose |
|------|---------|
| `Dockerfile.api` | Docker config for Render (port 10000, Production) |
| `render.yaml` | Render Blueprint deployment config |
| `netlify.toml` | Netlify build config (dotnet publish + SPA redirects) |
| `wwwroot/_redirects` | SPA client-side routing fallback |
| `appsettings.Production.json` | Production CORS origins for Netlify domain |
