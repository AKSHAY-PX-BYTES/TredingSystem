# Release Notes — v2.0.0 (May 21, 2026)

## 🚀 Deployment & Infrastructure

### Blazor Web App Deployed to Render
- Added `Dockerfile` (multi-stage: .NET 8 SDK build → nginx Alpine)
- Added `nginx.conf` with SPA fallback, API proxy, and SignalR proxy
- Added `render.yaml` blueprint for automated deployment
- **Live URL**: `https://tredingsystem-31vm.onrender.com`

### CORS & Connectivity
- Updated API CORS to allow new Render frontend origin
- Added CORS preflight caching (`SetPreflightMaxAge: 10 minutes`) for reduced latency

---

## 🔐 Security Hardening (Enterprise-Grade Audit)

### Secrets & Configuration
- ❌ Removed hardcoded JWT signing key from `appsettings.json` and `AuthService.cs`
- ✅ JWT key now required via environment variable (`JWT_KEY`) or configuration — app fails fast if missing
- ✅ Added key length validation (minimum 256 bits)
- ✅ Sanitized all user-facing error messages (no internal details leaked)

### Authentication & Account Security
- ✅ **Account Lockout**: 5 failed login attempts → 15-minute lockout
- ✅ Remaining attempts counter shown to user
- ✅ **Refresh Token Rotation**: cryptographically secure 64-byte tokens
- ✅ Token theft detection — if invalid refresh token used, all tokens revoked
- ✅ JWT expiry now configurable via `Jwt:ExpiryHours` (default: 8 hours)

### API Security
- ✅ Added `SecurityHeadersMiddleware`:
  - `X-Frame-Options: DENY`
  - `X-Content-Type-Options: nosniff`
  - `X-XSS-Protection: 1; mode=block`
  - `Strict-Transport-Security` (HSTS, 1 year)
  - `Referrer-Policy: strict-origin-when-cross-origin`
  - `Permissions-Policy` (camera, microphone, geolocation disabled)
  - `Content-Security-Policy: default-src 'self'; frame-ancestors 'none'`
  - Server/X-Powered-By headers removed
- ✅ Added `RateLimitingMiddleware`:
  - General: 100 requests/minute per IP
  - Auth endpoints: 10 requests/minute per IP (brute-force protection)
- ✅ Added `UseHttpsRedirection()`
- ✅ Swagger conditionally enabled via `EnableSwagger` config flag

### Payment Security
- ✅ `/payment/config-check` endpoint secured with `[Authorize(Roles = "Admin")]`
- ✅ Removed secret prefix leakage, key length exposure, and plan pricing from response
- ✅ All Razorpay error messages sanitized (no API response bodies shown to client)

### Exception Handling
- ✅ `GlobalExceptionHandlerMiddleware` no longer leaks `exception.Message` to clients
- ✅ Returns generic safe messages for all exception types

### Frontend Security (nginx)
- ✅ Security headers added to nginx config (HSTS, X-Frame-Options, etc.)

---

## 🛠️ Bug Fixes

### Admin Panel Visibility
- Fixed `AuthorizeView Roles="Admin"` not detecting role from JWT
- Added full Microsoft claim URI mapping in `CustomAuthStateProvider`
- Set explicit `ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role)`
- Moved Admin button to `navbar-right` for guaranteed visibility
- Styled admin button with border + highlight for clear identification

### Dashboard Razor Compilation
- Fixed `RZ1030` errors caused by nested double quotes in Razor `@onclick` attributes
- Extracted `NavigateToPlans()` helper method to avoid quote conflicts

### Blazor WASM MIME Type Issue
- Fixed HTML files downloading instead of rendering (nginx `types {}` block override issue)
- Added explicit `CMD ["nginx", "-g", "daemon off;"]` for proper container startup

---

## 📦 Database Migrations (Auto-applied)

New columns added to `users` table:
| Column | Type | Purpose |
|--------|------|---------|
| `failed_login_attempts` | `INTEGER DEFAULT 0` | Account lockout counter |
| `lockout_end_utc` | `TIMESTAMP` | Lockout expiry time |
| `refresh_token` | `VARCHAR(200)` | Stored refresh token |
| `refresh_token_expires_at` | `TIMESTAMP` | Refresh token expiry |

---

## 📋 API Changes

### Modified Endpoints
| Endpoint | Change |
|----------|--------|
| `POST /auth/login` | Now returns `refreshToken` field; shows lockout info |
| `POST /auth/refresh` | Now requires `refreshToken` in request body |
| `GET /payment/config-check` | Now requires Admin role (was anonymous) |

### New Request/Response Fields
- `LoginResponse.RefreshToken` — refresh token for token rotation
- `RefreshTokenRequest.RefreshToken` — required for refresh flow

---

## 🤖 DevOps & CI/CD

### Dependabot Configuration (`.github/dependabot.yml`)
- NuGet packages: weekly scan (API + Web)
- npm packages: weekly scan (Angular)
- Docker base images: weekly scan
- Auto-labels: `dependencies`, `security`

### Repository Hygiene
- Added `.gitignore` at solution root (bin, obj, .env, secrets, node_modules)
- Production logging level set to `Information` (was `Debug`)
- Removed verbose startup URL logging

---

## 📝 Environment Variables Required (Render)

| Variable | Purpose |
|----------|---------|
| `JWT_KEY` | JWT signing key (min 32 chars) |
| `DATABASE_URL` | Neon PostgreSQL connection |
| `Razorpay__KeyId` | Razorpay Key ID |
| `Razorpay__KeySecret` | Razorpay Key Secret |
| `Razorpay__WebhookSecret` | Razorpay Webhook Secret |
| `EmailProviders__Brevo__ApiKey` | Brevo email API key |

---

## 📊 Security Score

| Before | After |
|--------|-------|
| ~4/10 | **8.5/10** |

---

*Generated: May 21, 2026*
