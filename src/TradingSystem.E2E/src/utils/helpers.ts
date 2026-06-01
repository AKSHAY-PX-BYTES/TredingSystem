import { Page, expect } from '@playwright/test';

/** Misc reusable test helpers. */

/** Wait until the Blazor app shell is mounted (loading splash gone). */
export async function waitForBlazor(page: Page): Promise<void> {
  await page
    .locator('.loading-screen')
    .waitFor({ state: 'detached', timeout: 30_000 })
    .catch(() => {});
}

/** Assert the page did not render a fatal Blazor error. */
export async function expectNoBlazorError(page: Page): Promise<void> {
  const errorUi = page.locator('#blazor-error-ui');
  const visible = await errorUi.isVisible().catch(() => false);
  expect(visible, 'Blazor unhandled error UI should not be visible').toBeFalsy();
}

/** Collect console errors during a test for later assertions. */
export function captureConsoleErrors(page: Page): string[] {
  const errors: string[] = [];
  page.on('console', (msg) => {
    if (msg.type() === 'error') errors.push(msg.text());
  });
  page.on('pageerror', (err) => errors.push(err.message));
  return errors;
}

/** Retry an async predicate until it returns true or times out. */
export async function waitFor(
  predicate: () => Promise<boolean>,
  { timeout = 10_000, interval = 250 } = {},
): Promise<boolean> {
  const end = Date.now() + timeout;
  while (Date.now() < end) {
    if (await predicate().catch(() => false)) return true;
    await new Promise((r) => setTimeout(r, interval));
  }
  return false;
}
