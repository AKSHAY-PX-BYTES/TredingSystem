import { APIRequestContext, request } from '@playwright/test';
import { env } from '../config/env';

/**
 * Thin wrapper around Playwright's APIRequestContext for backend
 * integration tests against the TradingSystem API.
 */
export class ApiClient {
  private context!: APIRequestContext;
  private token: string | null = null;

  static async create(): Promise<ApiClient> {
    const client = new ApiClient();
    client.context = await request.newContext({
      baseURL: env.apiBaseURL,
      ignoreHTTPSErrors: true,
      extraHTTPHeaders: { Accept: 'application/json' },
    });
    return client;
  }

  /** Authenticate and cache the JWT for subsequent calls. */
  async login(username: string, password: string): Promise<boolean> {
    const res = await this.context.post('/auth/login', {
      data: { username, password },
    });
    if (!res.ok()) return false;
    const body = await res.json().catch(() => null);
    this.token = body?.token ?? body?.data?.token ?? null;
    return Boolean(this.token);
  }

  private authHeaders(): Record<string, string> {
    return this.token ? { Authorization: `Bearer ${this.token}` } : {};
  }

  async get(path: string) {
    return this.context.get(path, { headers: this.authHeaders() });
  }

  async post(path: string, data?: unknown) {
    return this.context.post(path, { headers: this.authHeaders(), data });
  }

  async dispose(): Promise<void> {
    await this.context.dispose();
  }
}
