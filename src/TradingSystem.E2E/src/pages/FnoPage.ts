import { Page, Locator } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * FnoPage — F&O list ("/fno") and detail ("/fno/{symbol}") screens.
 * Validates the live/estimated/unavailable data-provenance banners we added
 * to the options chain.
 */
export class FnoPage extends BasePage {
  readonly path = '/fno';

  readonly indexCards: Locator;
  readonly optionsSection: Locator;
  readonly chainTable: Locator;
  readonly liveBanner: Locator;
  readonly estimatedBanner: Locator;
  readonly unavailableBanner: Locator;
  readonly expiryButtons: Locator;
  readonly premiumOverlay: Locator;

  constructor(page: Page) {
    super(page);
    this.indexCards = page.locator('.fno-index-card, .index-card, a[href^="/fno/"]');
    this.optionsSection = page.locator('.options-section');
    this.chainTable = page.locator('.chain-table');
    this.liveBanner = page.locator('.market-live-banner');
    this.estimatedBanner = page.locator('.market-estimated-banner');
    this.unavailableBanner = page.locator('.market-unavailable');
    this.expiryButtons = page.locator('.expiry-btn');
    this.premiumOverlay = page.locator('.premium-locked-overlay');
  }

  /** Navigate directly to a symbol's detail page (e.g. "NIFTY50"). */
  async gotoSymbol(symbol: string): Promise<void> {
    await this.page.goto(`/fno/${symbol}`, { waitUntil: 'domcontentloaded' });
    await this.waitForAppReady();
  }

  async openFirstIndex(): Promise<void> {
    await this.indexCards.first().click();
  }

  async selectExpiry(index = 0): Promise<void> {
    const btn = this.expiryButtons.nth(index);
    if (await btn.isVisible().catch(() => false)) {
      await btn.click();
    }
  }

  /** Returns the data-provenance state shown on the chain. */
  async dataState(): Promise<'live' | 'estimated' | 'unavailable' | 'unknown'> {
    if (await this.liveBanner.isVisible().catch(() => false)) return 'live';
    if (await this.estimatedBanner.isVisible().catch(() => false)) return 'estimated';
    if (await this.unavailableBanner.isVisible().catch(() => false)) return 'unavailable';
    return 'unknown';
  }
}
