import 'dotenv/config';

/**
 * Centralized, environment-driven configuration for the E2E suite.
 * All values come from environment variables (see .env.example),
 * with sensible defaults so the suite can run out-of-the-box against
 * the deployed environment.
 */
export const env = {
  baseURL: process.env.BASE_URL ?? 'https://tredingsystem-31vm.onrender.com',
  apiBaseURL: process.env.API_BASE_URL ?? 'https://tredingsystem-api.onrender.com',
  testEnv: process.env.TEST_ENV ?? 'production',

  credentials: {
    // Defaults point at the API's seeded "trader" account (Pro plan) so the
    // suite runs end-to-end with zero skips out of the box. Override with real
    // secrets in CI (TEST_USERNAME/TEST_PASSWORD) for a dedicated account.
    username: process.env.TEST_USERNAME ?? 'trader',
    password: process.env.TEST_PASSWORD ?? 'Trader@123',
  },

  admin: {
    // Seeded admin account — bypasses premium gating so admin-only flows run.
    username: process.env.ADMIN_USERNAME ?? 'admin',
    password: process.env.ADMIN_PASSWORD ?? 'Admin@123',
  },

  workers: process.env.WORKERS ? Number(process.env.WORKERS) : undefined,
  testTimeout: Number(process.env.TEST_TIMEOUT ?? 60_000),
  retries: Number(process.env.RETRIES ?? 1),
} as const;

/** True when valid standard-user credentials are configured. */
export const hasUserCreds = (): boolean =>
  Boolean(env.credentials.username && env.credentials.password);

/** True when valid admin credentials are configured. */
export const hasAdminCreds = (): boolean =>
  Boolean(env.admin.username && env.admin.password);
