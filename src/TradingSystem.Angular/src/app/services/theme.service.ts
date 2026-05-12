import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly STORAGE_KEY = 'trading_theme';
  private themeSubject = new BehaviorSubject<string>(this.getSavedTheme());

  theme$ = this.themeSubject.asObservable();

  get currentTheme(): string { return this.themeSubject.value; }
  get isDarkMode(): boolean { return this.themeSubject.value === 'dark'; }

  initialize(): void {
    this.applyTheme(this.themeSubject.value);
  }

  toggleTheme(): void {
    const newTheme = this.isDarkMode ? 'light' : 'dark';
    this.setTheme(newTheme);
  }

  setTheme(theme: string): void {
    if (theme !== 'dark' && theme !== 'light') return;
    localStorage.setItem(this.STORAGE_KEY, theme);
    this.themeSubject.next(theme);
    this.applyTheme(theme);
  }

  private applyTheme(theme: string): void {
    document.documentElement.setAttribute('data-theme', theme);
  }

  private getSavedTheme(): string {
    return localStorage.getItem(this.STORAGE_KEY) || 'dark';
  }
}
