# E2E Test Coverage Matrix

Coverage of the TradingSystem web application by module and scenario type.
Legend: ✅ covered · ⚠️ conditional (feature-gated / credential-dependent, auto-skips) · ➖ N/A

## Modules × Scenario types

| Module / Area | Auth | Navigation | Forms & Validation | CRUD | API | Workflow | Responsive | Negative/Edge | Regression |
|---------------|:----:|:----------:|:------------------:|:----:|:---:|:--------:|:----------:|:-------------:|:----------:|
| **Login / Auth** | ✅ | ✅ | ✅ | ➖ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Registration (OTP)** | ⚠️ | ✅ | ✅ | ➖ | ➖ | ⚠️ | ➖ | ✅ | ⚠️ |
| **Authorization / Route guards** | ✅ | ✅ | ➖ | ➖ | ✅ | ➖ | ➖ | ✅ | ✅ |
| **Dashboard / Explore** | ⚠️ | ✅ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ✅ |
| **Markets** | ⚠️ | ✅ | ⚠️ | ➖ | ➖ | ✅ | ➖ | ⚠️ | ✅ |
| **F&O Options chain** | ⚠️ | ✅ | ➖ | ➖ | ✅ | ✅ | ➖ | ✅ | ✅ |
| **Watchlist** | ⚠️ | ✅ | ➖ | ⚠️ | ➖ | ➖ | ➖ | ➖ | ✅ |
| **Charts** | ⚠️ | ✅ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ✅ |
| **Settings** | ⚠️ | ✅ | ⚠️ | ⚠️ | ➖ | ➖ | ➖ | ➖ | ✅ |
| **Theme / Currency** | ⚠️ | ✅ | ➖ | ➖ | ➖ | ✅ | ➖ | ➖ | ✅ |
| **API backend** | ✅ | ➖ | ➖ | ➖ | ✅ | ✅ | ➖ | ✅ | ✅ |
| **Responsive (all viewports)** | ➖ | ➖ | ✅ | ➖ | ➖ | ➖ | ✅ | ✅ | ✅ |

## Test files → scenarios

| File | Scenarios |
|------|-----------|
| `tests/auth/login.spec.ts` | Render, masked password, invalid creds, empty submit, valid login, forgot-password open |
| `tests/auth/register.spec.ts` | Email step render, invalid email rejection, valid email → OTP request |
| `tests/auth/authorization.spec.ts` | Protected routes redirect to login (7 routes), authenticated shell, admin link |
| `tests/navigation/routing.spec.ts` | Unknown route no-crash, sidebar nav, F&O deep link, theme toggle |
| `tests/forms/validation.spec.ts` | Required fields, password toggle, forgot-password validation, in-flight disabled |
| `tests/fno/options-chain.spec.ts` | Index list, NIFTY50 data-state (LIVE/ESTIMATED/UNAVAILABLE), expiry select, 4 symbol deep links |
| `tests/markets/markets.spec.ts` | Page load, search filtering, premium gating |
| `tests/settings/settings.spec.ts` | Sections render, save control present |
| `tests/api/api-integration.spec.ts` | Reachability, invalid login, valid login, indices auth, options-chain `source` field |
| `tests/responsive/responsive.spec.ts` | mobile/tablet/desktop render + no overflow, keyboard usability |
| `tests/regression/smoke.spec.ts` | Public pages, authenticated core pages, logout |

## Scenario-type coverage summary

- **User authentication & authorization** — login (valid/invalid/empty), route guards on 7 protected routes, API-level auth, logout.
- **Navigation & routing** — sidebar across modules, deep links, unknown-route resilience, theme switching.
- **Forms, validation & error handling** — required fields, email-format rejection, password visibility, forgot-password, in-flight states.
- **CRUD operations** — settings/watchlist read & update paths (conditional on auth).
- **API integrations** — health, auth contract, F&O endpoints incl. data-provenance `source`.
- **Business workflows** — end-to-end F&O options-chain review with explicit live/estimated/unavailable states; markets browsing.
- **UI responsiveness & usability** — three viewports, no horizontal overflow, keyboard navigation.
- **Edge cases & negative scenarios** — bad credentials, invalid emails, unknown routes, premium-gated areas, market-data-unavailable.
- **Regression** — broad multi-page smoke for crashes/console errors.

## Notes on conditional (⚠️) coverage
Several tests depend on runtime state of the **live** target environment:
- **Credentials** — set `TEST_USERNAME`/`TEST_PASSWORD` to activate authenticated flows; otherwise they auto-skip.
- **Feature flags / plan** — premium-gated modules (Markets, F&O detail) are detected and skipped rather than failed.
- **Market hours / live feed** — F&O asserts the *state is explicit* (never a silent wrong value), valid in all conditions.
