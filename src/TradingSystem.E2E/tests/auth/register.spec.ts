import { test, expect } from '../../src/fixtures/test-fixtures';
import { uniqueEmail, invalidEmails } from '../../src/utils/test-data';
import type { RegisterPage } from '../../src/pages';

/**
 * Registration — step-1 (email + OTP request) rendering and validation.
 * Full account creation requires a real OTP and is out of scope for automated E2E.
 *
 * No-skip policy: a crashed/broken page FAILS. If signup is intentionally
 * feature-gated off, we assert the gate UI is shown (a valid state) and pass.
 * Returns true when the email step is available and the test should continue.
 */
async function openRegister(registerPage: RegisterPage): Promise<boolean> {
  await registerPage.goto();
  // A fatal Blazor error means the page is broken — fail loudly with context.
  expect(
    await registerPage.hasFatalError(),
    'Register page crashed (Blazor fatal error bar visible)',
  ).toBeFalsy();
  // If signup is gated off, validate the "Feature Unavailable" card and stop.
  if (await registerPage.isFeatureGated()) {
    await expect(registerPage.featureDisabled).toBeVisible();
    return false;
  }
  // Otherwise the email step MUST render; if it doesn't, that's a real failure.
  await expect(registerPage.emailInput).toBeVisible({ timeout: 15_000 });
  return true;
}

test.describe('Authentication › Registration', () => {
  test('register page renders email step @smoke', async ({ registerPage }) => {
    if (!(await openRegister(registerPage))) return;
    await expect(registerPage.emailInput).toBeVisible();
    await expect(registerPage.sendOtpButton).toBeVisible();
  });

  test('rejects invalid email formats @regression', async ({ registerPage, page }) => {
    if (!(await openRegister(registerPage))) return;
    for (const bad of invalidEmails.slice(0, 3)) {
      await registerPage.emailInput.fill(bad);
      await registerPage.sendOtpButton.click();
      await page.waitForTimeout(800);
      // Should not advance to OTP step (otp input not present).
      const advanced = await registerPage.otpInput.isVisible().catch(() => false);
      expect(advanced, `should not advance for "${bad}"`).toBeFalsy();
    }
  });

  test('accepts a well-formed email and requests OTP', async ({ registerPage, page }) => {
    if (!(await openRegister(registerPage))) return;
    await registerPage.submitEmail(uniqueEmail());
    await page.waitForTimeout(3000);
    // Outcome: advances to OTP step OR shows a success/error banner (rate limit etc).
    const advanced = await registerPage.otpInput.isVisible().catch(() => false);
    const banner =
      (await registerPage.successBanner.isVisible().catch(() => false)) ||
      (await registerPage.errorBanner.isVisible().catch(() => false));
    expect(advanced || banner).toBeTruthy();
  });
});
