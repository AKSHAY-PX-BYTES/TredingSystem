# TradingSystem E2E — Playwright Automation

Enterprise-grade, fully automated **End-to-End** test suite for the TradingSystem web
application (Blazor WASM frontend + ASP.NET Core API). Runs **headless**, generates a
**professional HTML report**, **emails results**, and integrates with **CI/CD**.

---

## 1. Tech stack

| Concern | Choice |
|--------|--------|
| Runner | [Playwright Test](https://playwright.dev) (`@playwright/test`) |
| Language | TypeScript (ESM) |
| Pattern | Page Object Model (POM) + custom fixtures |
| Reporting | Built-in **HTML** + JSON + JUnit |
| Email | `nodemailer` (SMTP) |
| CI/CD | GitHub Actions (`.github/workflows/e2e-tests.yml`) |
| Browsers | Chromium (responsive tests emulate mobile/tablet viewports) |

---

## 2. Project structure

```
TradingSystem.E2E/
├── playwright.config.ts        # Headless, multi-browser, HTML/JSON/JUnit reporters
├── package.json                # Scripts + dependencies
├── tsconfig.json
├── .env.example                # Copy to .env and fill in
├── src/
│   ├── config/env.ts           # Env-driven configuration
│   ├── pages/                  # Page Object Model
│   │   ├── BasePage.ts
│   │   ├── LoginPage.ts  RegisterPage.ts  DashboardPage.ts
│   │   ├── MarketsPage.ts  FnoPage.ts  SettingsPage.ts  NavBar.ts
│   │   └── index.ts
│   ├── fixtures/test-fixtures.ts  # Injects page objects + authedPage
│   └── utils/                  # api-client, helpers, test-data
├── tests/
│   ├── auth/                   # login, register, authorization
│   ├── navigation/             # routing, theme
│   ├── forms/                  # validation & error handling
│   ├── fno/                    # options-chain business workflow
│   ├── markets/                # markets module
│   ├── settings/               # settings CRUD
│   ├── api/                    # backend API integration
│   ├── responsive/             # viewport / usability
│   └── regression/             # broad smoke pass
├── scripts/send-report-email.ts   # Emails HTML summary after a run
└── reports/                    # Generated (HTML/JSON/JUnit) — git-ignored
```

---

## 3. Setup

> Requires **Node.js 20+**. Nothing else to install globally.

```powershell
cd src/TradingSystem.E2E
npm install
npx playwright install --with-deps   # downloads browser binaries
copy .env.example .env               # then edit .env
```

Fill in `.env`:
- `BASE_URL` / `API_BASE_URL` — target frontend/backend (defaults point to production).
- `TEST_USERNAME` / `TEST_PASSWORD` — a **dedicated disposable** test account. Without it,
  auth-dependent tests **auto-skip** (they never fail the build).
- `SMTP_*` / `EMAIL_TO` — optional, enables email reporting.

---

## 4. Running tests

```powershell
npm test                 # all tests, all projects, headless
npm run test:smoke       # only @smoke tagged
npm run test:regression  # only @regression tagged
npm run test:chromium    # single browser (fastest)
npm run test:auth        # a single folder
npm run test:headed      # watch it run in a browser
npm run report           # open the last HTML report
```

Filter examples:
```powershell
npx playwright test tests/fno --project=chromium
npx playwright test -g "options chain"
```

---

## 5. Reporting

After any run:
- **HTML dashboard** → `reports/html/index.html` (`npm run report` to open).
- **JSON** → `reports/results.json` (consumed by the email script & CI gate).
- **JUnit** → `reports/junit.xml` (for CI test-result panels).
- On failure: **screenshots, videos, and traces** are captured automatically
  (`test-results/`). Open a trace with `npx playwright show-trace <trace.zip>`.

### Email report
```powershell
npm run email:report     # run AFTER `npm test`
```
Sends a styled HTML summary (totals, pass rate, duration, failed-test details) and
attaches the full HTML report. Skips gracefully if SMTP isn't configured.

> **Gmail tip:** use an **App Password** (not your account password) and
> `SMTP_HOST=smtp.gmail.com`, `SMTP_PORT=587`, `SMTP_SECURE=false`.

---

## 6. CI/CD (GitHub Actions)

Workflow: **`.github/workflows/e2e-tests.yml`** (repo root). Triggers on:
- push to `main`/`develop`
- pull requests
- **deployment success** (`deployment_status`)
- daily schedule (02:30 UTC)
- manual dispatch (with optional `base_url` input)

It installs deps + browsers, runs **headless**, uploads the **HTML report** and
**traces** as artifacts, emails results, and **fails the job** if any test failed.

### Required GitHub configuration
Add under **Settings → Secrets and variables → Actions**:

**Variables** (optional, have defaults):
- `E2E_BASE_URL`, `E2E_API_BASE_URL`, `E2E_TEST_ENV`

**Secrets** (optional but recommended):
- `E2E_TEST_USERNAME`, `E2E_TEST_PASSWORD` (enables auth tests)
- `E2E_ADMIN_USERNAME`, `E2E_ADMIN_PASSWORD`
- Email: `E2E_SMTP_HOST`, `E2E_SMTP_PORT`, `E2E_SMTP_SECURE`, `E2E_SMTP_USER`,
  `E2E_SMTP_PASS`, `E2E_EMAIL_FROM`, `E2E_EMAIL_TO`

---

## 7. Design notes

- **Resilient against environment state.** The suite targets a live, deployed app where
  data and feature flags vary (free vs premium users, market hours). Tests assert on
  **stable contracts** (no fatal Blazor error, explicit data-provenance state, correct
  routing/redirects) and **skip** gracefully when a feature is gated or credentials are
  absent — they never produce false failures.
- **Blazor-aware.** `BasePage.waitForAppReady()` waits for the WASM loading splash to
  detach before interacting.
- **Parallel + sharded.** `fullyParallel: true`; CI uses multiple workers.

See **COVERAGE.md** for the module/scenario coverage matrix.
