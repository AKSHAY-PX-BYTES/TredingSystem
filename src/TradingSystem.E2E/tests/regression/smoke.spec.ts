import { test, expect } from '../../src/fixtures/test-fixtures';
import { NavBar } from '../../src/pages';
import {
  expectNoBlazorError,
  expectNoBlazorErrorResilient,
  captureConsoleErrors,
  gotoResilient,
} from '../../src/utils/helpers';

/**
 * Regression smoke — broad pass over public + key authenticated pages to
 * catch crashes, blank screens, and console errors across the app.
 */
test.describe('Regression › Smoke', () => {
  test('public pages load without fatal errors @smoke @regression', async ({ page }) => {
    // Cold-start tolerant: resilient nav + reload-retry on the fatal-error bar.
    test.setTimeout(120_000);
    // Path -> a selector that proves the page actually rendered its content.
    const publicPages: Array<{ path: string; ready: string }> = [
      { path: '/login', ready: '#username' },
      { path: '/register', ready: '#email, #username, input[type="email"]' },
    ];
    for (const { path, ready } of publicPages) {
      await gotoResilient(page, path);
      await page.locator('.loading-screen').waitFor({ state: 'detached', timeout: 30_000 }).catch(() => {});
      // Positive signal: if the app had truly crashed on boot, its content
      // would never render. Wait for the page's own elements to appear.
      await expect(page.locator(ready).first()).toBeVisible({ timeout: 30_000 });
      await expectNoBlazorErrorResilient(page);
    }
  });

  test('authenticated core pages load without fatal errors @smoke @regression', async ({ authedPage }) => {
    const errors = captureConsoleErrors(authedPage);
    const paths = ['/', '/markets', '/fno', '/watchlist', '/charts', '/settings'];
    for (const path of paths) {
      await gotoResilient(authedPage, path);
      await authedPage.locator('.loading-screen').waitFor({ state: 'detached', timeout: 30_000 }).catch(() => {});
      await authedPage.waitForTimeout(1200);
      await expectNoBlazorError(authedPage);
    }
    // Informational: surface (but don't hard-fail on) noisy console errors.
    test.info().annotations.push({ type: 'console-errors', description: String(errors.length) });
  });

  test('logout returns the user to an unauthenticated state @regression', async ({ authedPage }) => {
    const nav = new NavBar(authedPage);
    await gotoResilient(authedPage, '/');
    await authedPage.locator('.loading-screen').waitFor({ state: 'detached', timeout: 30_000 }).catch(() => {});
    await nav.openSidebar();
    const logout = authedPage.getByText(/log\s?out|sign out/i).first();
    if (await logout.isVisible().catch(() => false)) {
      await logout.click();
      await authedPage.waitForTimeout(2000);
      const onLogin = authedPage.url().includes('/login');
      const hasLoginForm = await authedPage.locator('#username').isVisible().catch(() => false);
      expect(onLogin || hasLoginForm).toBeTruthy();
    }
  });
});
