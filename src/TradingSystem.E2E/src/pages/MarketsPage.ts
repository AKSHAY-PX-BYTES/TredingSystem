import { Page, Locator } from '@playwright/test';
import { BasePage } from './BasePage';

/** MarketsPage — route "/markets". */
export class MarketsPage extends BasePage {
  readonly path = '/markets';

  readonly searchBox: Locator;
  readonly tabs: Locator;
  readonly rows: Locator;
  readonly premiumOverlay: Locator;

  constructor(page: Page) {
    super(page);
    this.searchBox = page.locator('input[type="search"], input[placeholder*="Search" i]');
    this.tabs = page.locator('.tab, .market-tab, [role="tab"]');
    this.rows = page.locator('table tbody tr, .market-row, .instrument-row');
    this.premiumOverlay = page.locator('.premium-locked-overlay');
  }

  async search(term: string): Promise<void> {
    if (await this.searchBox.first().isVisible().catch(() => false)) {
      await this.searchBox.first().fill(term);
    }
  }

  async rowCount(): Promise<number> {
    return this.rows.count();
  }

  async isPremiumLocked(): Promise<boolean> {
    return this.premiumOverlay.first().isVisible().catch(() => false);
  }
}
