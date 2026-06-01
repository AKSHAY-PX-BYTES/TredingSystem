import { Page, Locator } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * RegisterPage — encapsulates the multi-step /register flow.
 * Step 1: Email + "Send OTP"   (#email)
 * Step 2: OTP entry            (#otp)
 * Step 3: Phone (optional)
 * Step 4: Account details
 *
 * Full account creation requires a real OTP, so E2E focuses on step-1
 * rendering and client-side validation.
 */
export class RegisterPage extends BasePage {
  readonly path = '/register';

  readonly emailInput: Locator;
  readonly sendOtpButton: Locator;
  readonly otpInput: Locator;
  readonly errorBanner: Locator;
  readonly successBanner: Locator;

  constructor(page: Page) {
    super(page);
    this.emailInput = page.locator('#email');
    this.sendOtpButton = page.getByRole('button', { name: /send otp/i });
    this.otpInput = page.locator('#otp');
    this.errorBanner = page.locator('.login-error');
    this.successBanner = page.locator('.login-success');
  }

  async enterEmail(email: string): Promise<void> {
    await this.emailInput.fill(email);
  }

  async submitEmail(email: string): Promise<void> {
    await this.enterEmail(email);
    await this.sendOtpButton.click();
  }

  async getErrorText(): Promise<string> {
    return (await this.errorBanner.textContent())?.trim() ?? '';
  }
}
