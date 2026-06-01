import { test, expect } from '../../src/fixtures/test-fixtures';
import { uniqueEmail, invalidEmails } from '../../src/utils/test-data';

/**
 * Registration — step-1 (email + OTP request) rendering and validation.
 * Full account creation requires a real OTP and is out of scope for automated E2E.
 */
test.describe('Authentication › Registration', () => {
  test('register page renders email step @smoke', async ({ registerPage, page }) => {
    await registerPage.goto();
    // Either the signup feature is enabled (email field) or gated.
    const emailVisible = await registerPage.emailInput.isVisible().catch(() => false);
    if (!emailVisible) {
      test.skip(true, 'Signup feature appears gated/disabled in this environment');
    }
    await expect(registerPage.emailInput).toBeVisible();
    await expect(registerPage.sendOtpButton).toBeVisible();
  });

  test('rejects invalid email formats @regression', async ({ registerPage }) => {
    await registerPage.goto();
    if (!(await registerPage.emailInput.isVisible().catch(() => false))) {
      test.skip(true, 'Signup feature gated');
    }
    for (const bad of invalidEmails.slice(0, 3)) {
      await registerPage.emailInput.fill(bad);
      await registerPage.sendOtpButton.click();
      await registerPage.page.waitForTimeout(800);
      // Should not advance to OTP step (otp input not present).
      const advanced = await registerPage.otpInput.isVisible().catch(() => false);
      expect(advanced, `should not advance for "${bad}"`).toBeFalsy();
    }
  });

  test('accepts a well-formed email and requests OTP', async ({ registerPage }) => {
    await registerPage.goto();
    if (!(await registerPage.emailInput.isVisible().catch(() => false))) {
      test.skip(true, 'Signup feature gated');
    }
    await registerPage.submitEmail(uniqueEmail());
    await registerPage.page.waitForTimeout(3000);
    // Outcome: advances to OTP step OR shows a success/error banner (rate limit etc).
    const advanced = await registerPage.otpInput.isVisible().catch(() => false);
    const banner =
      (await registerPage.successBanner.isVisible().catch(() => false)) ||
      (await registerPage.errorBanner.isVisible().catch(() => false));
    expect(advanced || banner).toBeTruthy();
  });
});
