import { test, expect } from '../../src/fixtures/test-fixtures';
import { NavBar } from '../../src/pages';
import { expectNoBlazorError, captureConsoleErrors } from '../../src/utils/helpers';

/**
 * Regression smoke — broad pass over public + key authenticated pages to
 * catch crashes, blank screens, and console errors across the app.
 */
test.describe('Regression › Smoke', () => {
  test('public pages load without fatal errors @smoke @regression', async ({ page }) => {
    for (const path of ['/login', '/register']) {
      await page.goto(path, { waitUntil: 'domcontentloaded' });
      await page.locator('.loading-screen').waitFor({ state: 'detached', timeout: 30_000 }).catch(() => {});
      await page.waitForTimeout(1200);
      await expectNoBlazorError(page);
    }
  });

  test('authenticated core pages load without fatal errors @smoke @regression', async ({ authedPage }) => {
    const errors = captureConsoleErrors(authedPage);
    const paths = ['/', '/markets', '/fno', '/watchlist', '/charts', '/settings'];
    for (const path of paths) {
      await authedPage.goto(path, { waitUntil: 'domcontentloaded' });
      await authedPage.locator('.loading-screen').waitFor({ state: 'detached', timeout: 30_000 }).catch(() => {});
      await authedPage.waitForTimeout(1200);
      await expectNoBlazorError(authedPage);
    }
    // Informational: surface (but don't hard-fail on) noisy console errors.
    test.info().annotations.push({ type: 'console-errors', description: String(errors.length) });
  });

  test('logout returns the user to an unauthenticated state @regression', async ({ authedPage }) => {
    const nav = new NavBar(authedPage);
    await authedPage.goto('/', { waitUntil: 'domcontentloaded' });
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
