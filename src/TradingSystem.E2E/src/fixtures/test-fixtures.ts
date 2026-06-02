import { test as base } from '@playwright/test';
import {
  LoginPage,
  RegisterPage,
  DashboardPage,
  MarketsPage,
  FnoPage,
  SettingsPage,
  NavBar,
} from '../pages';
import { env } from '../config/env';

/**
 * Custom fixtures: inject page objects and an authenticated session.
 * Credentials default to the API's seeded "trader" account (see config/env),
 * so the suite runs end-to-end without skipping. Override via env/secrets.
 */
type Pages = {
  loginPage: LoginPage;
  registerPage: RegisterPage;
  dashboardPage: DashboardPage;
  marketsPage: MarketsPage;
  fnoPage: FnoPage;
  settingsPage: SettingsPage;
  navBar: NavBar;
};

type AuthFixtures = {
  /** A page that has been logged in with the configured test user. */
  authedPage: import('@playwright/test').Page;
};

export const test = base.extend<Pages & AuthFixtures>({
  loginPage: async ({ page }, use) => use(new LoginPage(page)),
  registerPage: async ({ page }, use) => use(new RegisterPage(page)),
  dashboardPage: async ({ page }, use) => use(new DashboardPage(page)),
  marketsPage: async ({ page }, use) => use(new MarketsPage(page)),
  fnoPage: async ({ page }, use) => use(new FnoPage(page)),
  settingsPage: async ({ page }, use) => use(new SettingsPage(page)),
  navBar: async ({ page }, use) => use(new NavBar(page)),

  authedPage: async ({ page }, use) => {
    // No skip: credentials always resolve to a seeded dummy account by default.
    const login = new LoginPage(page);
    await login.goto();
    await login.loginAndWait(env.credentials.username, env.credentials.password);
    await use(page);
  },
});

export const expect = test.expect;
