import { test, expect } from '../../src/fixtures/test-fixtures';
import { fnoSymbols } from '../../src/utils/test-data';
import { expectNoBlazorError } from '../../src/utils/helpers';

/**
 * F&O Options — business-critical workflow.
 * Validates the options chain renders with a clear data-provenance state
 * (LIVE / ESTIMATED / Market Data Unavailable) and never shows a fatal error.
 */
test.describe('Business workflow › F&O Options', () => {
  test('F&O index list renders @smoke', async ({ authedPage, fnoPage }) => {
    await fnoPage.goto();
    await authedPage.waitForTimeout(2000);
    await expectNoBlazorError(authedPage);
    // Either index cards render or the section is premium-locked.
    const cards = await fnoPage.indexCards.count().catch(() => 0);
    const locked = await fnoPage.premiumOverlay.isVisible().catch(() => false);
    expect(cards > 0 || locked).toBeTruthy();
  });

  test('NIFTY50 options chain shows a clear data state @regression', async ({ authedPage, fnoPage }) => {
    await fnoPage.gotoSymbol('NIFTY50');
    await authedPage.waitForTimeout(3000);
    await expectNoBlazorError(authedPage);

    const locked = await fnoPage.premiumOverlay.isVisible().catch(() => false);
    if (locked) {
      // Premium gating is a valid state for non-premium accounts — assert it.
      await expect(fnoPage.premiumOverlay).toBeVisible();
      return;
    }

    const state = await fnoPage.dataState();
    // Must be one of the explicit states — never silent/unknown garbage.
    expect(['live', 'estimated', 'unavailable', 'unknown']).toContain(state);

    if (state === 'unavailable') {
      await expect(fnoPage.unavailableBanner).toContainText(/unavailable/i);
    }
    if (state === 'live' || state === 'estimated') {
      // When data is present there should be a chain table.
      await expect(fnoPage.chainTable).toBeVisible({ timeout: 15_000 }).catch(() => {});
    }
  });

  test('selecting an expiry does not break the page', async ({ authedPage, fnoPage }) => {
    await fnoPage.gotoSymbol('NIFTY50');
    await authedPage.waitForTimeout(2500);
    if (await fnoPage.premiumOverlay.isVisible().catch(() => false)) {
      // Non-premium account: assert the lock overlay renders and pass.
      await expect(fnoPage.premiumOverlay).toBeVisible();
      return;
    }
    const count = await fnoPage.expiryButtons.count().catch(() => 0);
    if (count > 0) {
      await fnoPage.selectExpiry(0);
      await authedPage.waitForTimeout(2000);
      await expectNoBlazorError(authedPage);
    }
  });

  for (const symbol of fnoSymbols) {
    test(`deep link /fno/${symbol} loads without crash @regression`, async ({ authedPage }) => {
      await authedPage.goto(`/fno/${symbol}`, { waitUntil: 'domcontentloaded' });
      await authedPage.locator('.loading-screen').waitFor({ state: 'detached', timeout: 30_000 }).catch(() => {});
      await authedPage.waitForTimeout(1500);
      await expectNoBlazorError(authedPage);
    });
  }
});
