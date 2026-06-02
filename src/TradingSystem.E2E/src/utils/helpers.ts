import { Page, expect } from '@playwright/test';

/** Misc reusable test helpers. */

/**
 * Navigate to a path resiliently.
 *
 * The frontend is a Blazor WASM SPA hosted on Netlify's free tier, so the
 * first request after an idle period can be slow (cold start + multi-MB WASM
 * download). We use `waitUntil: 'commit'` (resolves as soon as the server
 * responds) instead of `domcontentloaded`/`load` so navigation doesn't block
 * on every asset, and we retry once on a slow/cold first hit.
 */
export async function gotoResilient(
  page: Page,
  path: string,
  { timeout = 60_000, retries = 1 }: { timeout?: number; retries?: number } = {},
): Promise<void> {
  let lastErr: unknown;
  for (let attempt = 0; attempt <= retries; attempt++) {
    try {
      await page.goto(path, { waitUntil: 'commit', timeout });
      return;
    } catch (err) {
      lastErr = err;
      // Brief pause to let a cold Netlify instance finish warming up.
      await page.waitForTimeout(1_000);
    }
  }
  throw lastErr;
}

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
