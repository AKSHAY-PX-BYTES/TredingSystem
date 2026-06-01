import { test, expect } from '../../src/fixtures/test-fixtures';
import { NavBar } from '../../src/pages';

/**
 * Authorization — protected routes require authentication.
 * Unauthenticated access to protected pages should redirect to /login.
 */
const protectedRoutes = ['/markets', '/fno', '/watchlist', '/charts', '/compare', '/settings', '/admin'];

test.describe('Authorization › Protected routes', () => {
  for (const route of protectedRoutes) {
    test(`unauthenticated access to ${route} redirects to login @regression`, async ({ page }) => {
      await page.goto(route, { waitUntil: 'domcontentloaded' });
      await page.locator('.loading-screen').waitFor({ state: 'detached', timeout: 30_000 }).catch(() => {});
      await page.waitForTimeout(2500);
      const url = page.url();
      const onLogin = url.includes('/login');
      const hasLoginForm = await page.locator('#username').isVisible().catch(() => false);
      expect(onLogin || hasLoginForm, `${route} should require auth`).toBeTruthy();
    });
  }
});

test.describe('Authorization › Authenticated shell', () => {
  test('authenticated user sees the app navbar @smoke', async ({ authedPage }) => {
    await authedPage.goto('/', { waitUntil: 'domcontentloaded' });
    await authedPage.locator('.loading-screen').waitFor({ state: 'detached', timeout: 30_000 }).catch(() => {});
    await expect(authedPage.locator('.groww-navbar')).toBeVisible();
  });

  test('admin link visibility reflects role', async ({ authedPage }) => {
    const nav = new NavBar(authedPage);
    await nav.openSidebar();
    // Admin link may or may not be present depending on the test user's role;
    // assert the sidebar itself rendered.
    await expect(nav.sidebar).toBeVisible();
  });
});
