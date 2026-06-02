import { Page, Locator, expect } from '@playwright/test';
import { gotoResilient } from '../utils/helpers';

/**
 * BasePage — shared behavior for all page objects.
 * Encapsulates navigation, waiting, and common Blazor-WASM quirks
 * (the app boots asynchronously, so we wait for the loading screen to clear).
 */
export abstract class BasePage {
  constructor(protected readonly page: Page) {}

  /** Relative path (e.g. "/markets") for this page; override in subclasses. */
  abstract readonly path: string;

  /** Navigate to this page's path and wait for the app to be interactive. */
  async goto(): Promise<void> {
    await gotoResilient(this.page, this.path);
    await this.waitForAppReady();
  }

  /**
   * Blazor WASM shows a "Loading Trading System..." splash until the runtime
   * is ready. Wait for it to disappear (or for any real content to appear).
   */
  async waitForAppReady(): Promise<void> {
    const splash = this.page.locator('.loading-screen');
    try {
      await splash.waitFor({ state: 'detached', timeout: 30_000 });
    } catch {
      // Some pages replace #app entirely; fall back to network idle.
      await this.page.waitForLoadState('networkidle').catch(() => {});
    }
  }

  async currentUrl(): Promise<string> {
    return this.page.url();
  }

  async title(): Promise<string> {
    return this.page.title();
  }

  /** Reusable visibility assertion helper. */
  async expectVisible(locator: Locator, timeout = 10_000): Promise<void> {
    await expect(locator).toBeVisible({ timeout });
  }

  /** True if at least one element matching the selector is visible. */
  async isVisible(selector: string): Promise<boolean> {
    return this.page
      .locator(selector)
      .first()
      .isVisible()
      .catch(() => false);
  }

  /** Click the first matching, visible element by text. */
  async clickByText(text: string): Promise<void> {
    await this.page.getByText(text, { exact: false }).first().click();
  }
}
