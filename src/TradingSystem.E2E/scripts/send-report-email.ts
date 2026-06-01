import 'dotenv/config';
import { readFileSync, existsSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import nodemailer from 'nodemailer';

/**
 * Reads the Playwright JSON results, builds an HTML summary, and emails it
 * (with the full HTML report attached as a zip-less inline summary) to the
 * configured recipients.
 *
 * Usage: npm run email:report   (run AFTER `playwright test`)
 */

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = resolve(__dirname, '..');

type Stats = { total: number; passed: number; failed: number; flaky: number; skipped: number; durationMs: number };

function parseResults(): { stats: Stats; failures: Array<{ title: string; error: string }> } {
  const jsonPath = resolve(root, 'reports/results.json');
  const stats: Stats = { total: 0, passed: 0, failed: 0, flaky: 0, skipped: 0, durationMs: 0 };
  const failures: Array<{ title: string; error: string }> = [];

  if (!existsSync(jsonPath)) {
    console.warn('No reports/results.json found — did the tests run?');
    return { stats, failures };
  }

  const report = JSON.parse(readFileSync(jsonPath, 'utf-8'));

  const walk = (suite: any, ancestors: string[] = []): void => {
    for (const s of suite.suites ?? []) walk(s, [...ancestors, s.title]);
    for (const spec of suite.specs ?? []) {
      for (const t of spec.tests ?? []) {
        const results = t.results ?? [];
        const last = results[results.length - 1];
        const status = last?.status ?? 'skipped';
        stats.total += 1;
        stats.durationMs += last?.duration ?? 0;
        if (status === 'passed') stats.passed += 1;
        else if (status === 'skipped') stats.skipped += 1;
        else if (t.status === 'flaky') stats.flaky += 1;
        else {
          stats.failed += 1;
          const errMsg = last?.error?.message ?? last?.errors?.[0]?.message ?? 'Unknown error';
          failures.push({ title: [...ancestors, spec.title].join(' › '), error: stripAnsi(errMsg).slice(0, 600) });
        }
      }
    }
  };

  for (const suite of report.suites ?? []) walk(suite, [suite.title]);
  return { stats, failures };
}

function stripAnsi(s: string): string {
  // eslint-disable-next-line no-control-regex
  return s.replace(/\u001b\[[0-9;]*m/g, '');
}

function fmtDuration(ms: number): string {
  const sec = Math.round(ms / 1000);
  const m = Math.floor(sec / 60);
  const s = sec % 60;
  return m > 0 ? `${m}m ${s}s` : `${s}s`;
}

function buildHtml(stats: Stats, failures: Array<{ title: string; error: string }>): string {
  const passRate = stats.total ? Math.round((stats.passed / stats.total) * 100) : 0;
  const status = stats.failed === 0 ? 'PASSED' : 'FAILED';
  const statusColor = stats.failed === 0 ? '#16a34a' : '#dc2626';
  const env = process.env.TEST_ENV ?? 'production';
  const baseUrl = process.env.BASE_URL ?? '';

  const failureRows = failures
    .map(
      (f) => `
      <tr>
        <td style="padding:8px;border-bottom:1px solid #eee;color:#dc2626;font-weight:600">${escapeHtml(f.title)}</td>
        <td style="padding:8px;border-bottom:1px solid #eee;"><pre style="margin:0;white-space:pre-wrap;font-size:12px;color:#444">${escapeHtml(f.error)}</pre></td>
      </tr>`,
    )
    .join('');

  return `
  <div style="font-family:Inter,Arial,sans-serif;max-width:760px;margin:auto;border:1px solid #eee;border-radius:12px;overflow:hidden">
    <div style="background:${statusColor};color:#fff;padding:20px 24px">
      <h1 style="margin:0;font-size:20px">TradingSystem E2E — ${status}</h1>
      <p style="margin:4px 0 0;opacity:.9;font-size:13px">Environment: ${env} &nbsp;•&nbsp; ${new Date().toUTCString()}</p>
    </div>
    <div style="display:flex;flex-wrap:wrap;gap:12px;padding:20px 24px">
      ${stat('Total', stats.total, '#111')}
      ${stat('Passed', stats.passed, '#16a34a')}
      ${stat('Failed', stats.failed, '#dc2626')}
      ${stat('Skipped', stats.skipped, '#9ca3af')}
      ${stat('Pass rate', passRate + '%', '#2563eb')}
      ${stat('Duration', fmtDuration(stats.durationMs), '#7c3aed')}
    </div>
    ${baseUrl ? `<p style="padding:0 24px;color:#666;font-size:13px">Target: <a href="${baseUrl}">${baseUrl}</a></p>` : ''}
    ${
      failures.length
        ? `<div style="padding:0 24px 24px">
            <h2 style="font-size:15px;color:#dc2626">Failed tests (${failures.length})</h2>
            <table style="width:100%;border-collapse:collapse;font-size:13px"><tbody>${failureRows}</tbody></table>
           </div>`
        : `<p style="padding:0 24px 24px;color:#16a34a;font-weight:600">All executed tests passed 🎉</p>`
    }
    <div style="background:#f9fafb;padding:14px 24px;color:#888;font-size:12px">
      Full HTML report is attached / available as a CI artifact (reports/html/index.html).
    </div>
  </div>`;
}

function stat(label: string, value: string | number, color: string): string {
  return `<div style="flex:1;min-width:100px;background:#f9fafb;border-radius:10px;padding:14px;text-align:center">
    <div style="font-size:22px;font-weight:700;color:${color}">${value}</div>
    <div style="font-size:12px;color:#6b7280;margin-top:2px">${label}</div>
  </div>`;
}

function escapeHtml(s: string): string {
  return s.replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]!));
}

async function main(): Promise<void> {
  const { stats, failures } = parseResults();
  const html = buildHtml(stats, failures);

  const to = process.env.EMAIL_TO?.trim();
  if (!to || !process.env.SMTP_HOST || !process.env.SMTP_USER) {
    console.log('Email not configured (EMAIL_TO/SMTP_* missing). Skipping send.');
    console.log(`Summary: ${stats.passed}/${stats.total} passed, ${stats.failed} failed.`);
    return;
  }

  const transporter = nodemailer.createTransport({
    host: process.env.SMTP_HOST,
    port: Number(process.env.SMTP_PORT ?? 587),
    secure: process.env.SMTP_SECURE === 'true',
    auth: { user: process.env.SMTP_USER, pass: process.env.SMTP_PASS },
  });

  const statusWord = stats.failed === 0 ? 'PASSED ✅' : 'FAILED ❌';
  const prefix = process.env.EMAIL_SUBJECT_PREFIX ?? '[TradingSystem E2E]';
  const subject = `${prefix} ${statusWord} — ${stats.passed}/${stats.total} passed (${process.env.TEST_ENV ?? 'production'})`;

  const attachments = [];
  const htmlReport = resolve(root, 'reports/html/index.html');
  if (existsSync(htmlReport)) {
    attachments.push({ filename: 'e2e-report.html', path: htmlReport });
  }

  await transporter.sendMail({
    from: process.env.EMAIL_FROM ?? process.env.SMTP_USER,
    to,
    subject,
    html,
    attachments,
  });

  console.log(`Report emailed to ${to}`);
}

main().catch((err) => {
  console.error('Failed to send report email:', err);
  process.exit(1);
});
