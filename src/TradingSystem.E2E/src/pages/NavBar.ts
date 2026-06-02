import { Page, Locator } from '@playwright/test';

/**
 * NavBar / Sidebar component object (MainLayout.razor).
 * Encapsulates the slide-out sidebar navigation used across the authenticated app.
 */
export class NavBar {
  readonly menuToggle: Locator;
  readonly sidebar: Locator;
  readonly themeToggle: Locator;
  readonly brand: Locator;

  /** Known sidebar destinations: label -> route path. */
  static readonly routes: Record<string, string> = {
    Explore: '/',
    Markets: '/markets',
    'F&O Options': '/fno',
    Watchlist: '/watchlist',
    Charts: '/charts',
    Compare: '/compare',
    Backtest: '/backtest',
    Settings: '/settings',
    Plans: '/plans',
    Notifications: '/notifications',
  };

  constructor(private readonly page: Page) {
    this.menuToggle = page.locator('.menu-toggle');
    this.sidebar = page.locator('aside.sidebar');
    this.themeToggle = page.locator('.theme-toggle');
    this.brand = page.locator('.navbar-brand');
  }

  async openSidebar(): Promise<void> {
    if (!(await this.sidebar.evaluate((el) => el.classList.contains('open')).catch(() => false))) {
      // Bounded click: if the toggle isn't present (e.g. unauthenticated), don't
      // block for the full action timeout — fail fast and let the caller decide.
      await this.menuToggle.click({ timeout: 5_000 }).catch(() => {});
      await this.sidebar.waitFor({ state: 'visible', timeout: 5_000 }).catch(() => {});
    }
  }

  /** Navigate via the sidebar link with the given visible label. */
  async navigateTo(label: string): Promise<void> {
    await this.openSidebar();
    await this.page.locator('.sidebar-link', { hasText: label }).first().click();
  }

  async toggleTheme(): Promise<void> {
    await this.themeToggle.click();
  }

  async currentTheme(): Promise<string | null> {
    return this.page
      .locator('.groww-app')
      .getAttribute('data-theme', { timeout: 5_000 })
      .catch(() => null);
  }
}
