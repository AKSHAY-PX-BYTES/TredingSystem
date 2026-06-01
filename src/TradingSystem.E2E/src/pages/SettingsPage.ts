import { Page, Locator } from '@playwright/test';
import { BasePage } from './BasePage';

/** SettingsPage — route "/settings". */
export class SettingsPage extends BasePage {
  readonly path = '/settings';

  readonly sections: Locator;
  readonly saveButtons: Locator;

  constructor(page: Page) {
    super(page);
    this.sections = page.locator('.settings-section, .settings-card, section');
    this.saveButtons = page.getByRole('button', { name: /save|update/i });
  }

  async sectionCount(): Promise<number> {
    return this.sections.count();
  }
}
