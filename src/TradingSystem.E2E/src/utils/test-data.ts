/** Deterministic + randomized test data helpers. */

export function uniqueEmail(prefix = 'qa.e2e'): string {
  const stamp = Date.now().toString(36);
  const rand = Math.random().toString(36).slice(2, 7);
  return `${prefix}+${stamp}${rand}@example.com`;
}

export function randomString(length = 8): string {
  const chars = 'abcdefghijklmnopqrstuvwxyz0123456789';
  let out = '';
  for (let i = 0; i < length; i++) out += chars[Math.floor(Math.random() * chars.length)];
  return out;
}

/** Invalid email samples for negative validation tests. */
export const invalidEmails = [
  'plainaddress',
  '@no-local.com',
  'no-at-sign.com',
  'spaces in@email.com',
  'double@@at.com',
];

/** F&O index symbols available in the app. */
export const fnoSymbols = ['NIFTY50', 'BANKNIFTY', 'FINNIFTY', 'SENSEX'];
