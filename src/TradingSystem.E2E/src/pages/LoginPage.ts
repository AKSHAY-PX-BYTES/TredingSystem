import { Page, Locator } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * LoginPage — encapsulates the /login screen.
 * Selectors match Login.razor: #username, #password, "Sign In" submit button.
 */
export class LoginPage extends BasePage {
  readonly path = '/login';

  readonly usernameInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly errorBanner: Locator;
  readonly forgotPasswordLink: Locator;
  readonly registerLink: Locator;

  constructor(page: Page) {
    super(page);
    this.usernameInput = page.locator('#username');
    this.passwordInput = page.locator('#password');
    this.submitButton = page.locator('button[type="submit"].login-btn, button.login-btn').first();
    this.errorBanner = page.locator('.login-error');
    this.forgotPasswordLink = page.getByText('Forgot Password', { exact: false });
    this.registerLink = page.getByRole('link', { name: /register|sign up|create account/i });
  }

  async login(username: string, password: string): Promise<void> {
    await this.usernameInput.fill(username);
    await this.passwordInput.fill(password);
    await this.submitButton.click();
  }

  /** Perform login and wait for navigation away from /login. */
  async loginAndWait(username: string, password: string): Promise<void> {
    await this.login(username, password);
    await this.page
      .waitForURL((url) => !url.pathname.includes('/login'), { timeout: 30_000 })
      .catch(() => {});
  }

  async getErrorText(): Promise<string> {
    return (await this.errorBanner.textContent())?.trim() ?? '';
  }

  async openForgotPassword(): Promise<void> {
    await this.forgotPasswordLink.first().click();
  }
}
