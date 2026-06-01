import { defineConfig, devices } from '@playwright/test';
import { env } from './src/config/env';

/**
 * Playwright configuration — headless, CI-ready, Chromium-only,
 * with professional HTML + JSON reporting and trace/screenshot/video on failure.
 * Responsive tests emulate mobile/tablet/desktop viewports within Chromium.
 *
 * Docs: https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  outputDir: './test-results',

  // Run all tests in parallel within a file and across files.
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? Math.max(env.retries, 2) : env.retries,
  workers: env.workers ?? (process.env.CI ? 2 : undefined),

  timeout: env.testTimeout,
  expect: { timeout: 10_000 },

  reporter: [
    ['list'],
    ['html', { outputFolder: 'reports/html', open: 'never' }],
    ['json', { outputFile: 'reports/results.json' }],
    ['junit', { outputFile: 'reports/junit.xml' }],
  ],

  use: {
    baseURL: env.baseURL,
    headless: true,
    actionTimeout: 15_000,
    navigationTimeout: 45_000,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    ignoreHTTPSErrors: true,
    testIdAttribute: 'data-testid',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
