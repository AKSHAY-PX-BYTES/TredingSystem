import { test, expect } from '@playwright/test';

/**
 * UI responsiveness & usability — verifies the app renders sensibly across
 * mobile, tablet, and desktop viewports. Runs against the public /login page
 * so it does not require credentials.
 */
const viewports = [
  { name: 'mobile', width: 375, height: 812 },
  { name: 'tablet', width: 768, height: 1024 },
  { name: 'desktop', width: 1440, height: 900 },
];

test.describe('UI › Responsiveness', () => {
  for (const vp of viewports) {
    test(`login renders correctly at ${vp.name} (${vp.width}x${vp.height}) @regression`, async ({ page }) => {
      await page.setViewportSize({ width: vp.width, height: vp.height });
      await page.goto('/login', { waitUntil: 'domcontentloaded' });
      await page.locator('.loading-screen').waitFor({ state: 'detached', timeout: 30_000 }).catch(() => {});

      const username = page.locator('#username');
      await expect(username).toBeVisible();

      // No horizontal scrollbar (content should fit the viewport width).
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
      );
      expect(overflow, 'page should not overflow horizontally').toBeLessThanOrEqual(2);
    });
  }

  test('login form is usable via keyboard (tab + type) @regression', async ({ page }) => {
    await page.goto('/login', { waitUntil: 'domcontentloaded' });
    await page.locator('.loading-screen').waitFor({ state: 'detached', timeout: 30_000 }).catch(() => {});
    await page.locator('#username').click();
    await page.keyboard.type('keyboard_user');
    await page.keyboard.press('Tab');
    await page.keyboard.type('keyboard_pass');
    await expect(page.locator('#username')).toHaveValue('keyboard_user');
  });
});
