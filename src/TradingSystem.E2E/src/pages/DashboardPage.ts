import { Page, Locator } from '@playwright/test';
import { BasePage } from './BasePage';

/** DashboardPage — the authenticated landing page ("Explore", route "/"). */
export class DashboardPage extends BasePage {
  readonly path = '/';

  readonly navbar: Locator;
  readonly userPill: Locator;

  constructor(page: Page) {
    super(page);
    this.navbar = page.locator('.groww-navbar');
    this.userPill = page.locator('.user-pill, .user-avatar-sm');
  }

  async isAuthenticatedShellVisible(): Promise<boolean> {
    return this.navbar.isVisible().catch(() => false);
  }
}
