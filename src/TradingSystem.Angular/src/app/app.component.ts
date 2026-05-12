import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { AuthService } from './core/services/auth.service';
import { ThemeService } from './services/theme.service';
import { CurrencyService } from './services/currency.service';
import { FeatureFlagService } from './services/feature-flag.service';
import { NotificationApiService } from './services/notification-api.service';
import { UserInfo, ChangePasswordRequest } from './models/models';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit, OnDestroy {
  user: UserInfo | null = null;
  userPlan = 'Free';
  isAdmin = false;
  unreadNotifications = 0;

  // Change password state
  showChangePassword = false;
  currentPassword = '';
  newPassword = '';
  confirmNewPassword = '';
  pwdErrorMessage = '';
  pwdSuccessMessage = '';
  isChangingPassword = false;

  private idleTimer: any;
  private idleWarningTimer: any;
  private readonly IDLE_TIMEOUT_MS = 10 * 60 * 1000; // 10 min
  private subs: Subscription[] = [];

  constructor(
    public authService: AuthService,
    public themeService: ThemeService,
    public currencyService: CurrencyService,
    public featureFlagService: FeatureFlagService,
    private notificationApi: NotificationApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.themeService.initialize();
    this.currencyService.loadCurrencies().subscribe();
    this.featureFlagService.loadFlags().subscribe();

    this.subs.push(
      this.authService.user$.subscribe(user => {
        this.user = user;
        this.userPlan = user?.plan || 'Free';
        this.isAdmin = user?.role === 'Admin';
        if (user) {
          this.notificationApi.getUnreadCount().subscribe(c => this.unreadNotifications = c);
          this.resetIdleTimer();
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
    this.clearIdleTimer();
  }

  isFeatureVisible(key: string): boolean {
    return this.isAdmin || this.featureFlagService.isEnabled(key);
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  onCurrencyChange(event: Event): void {
    const code = (event.target as HTMLSelectElement).value;
    this.currencyService.setCurrency(code);
  }

  logout(): void {
    this.clearIdleTimer();
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  handleChangePassword(): void {
    if (!this.cpCanSubmit) return;
    this.pwdErrorMessage = '';
    this.pwdSuccessMessage = '';
    this.isChangingPassword = true;

    const req: ChangePasswordRequest = {
      currentPassword: this.currentPassword,
      newPassword: this.newPassword,
      confirmNewPassword: this.confirmNewPassword
    };

    this.authService.changePassword(req).subscribe(result => {
      this.isChangingPassword = false;
      if (result.success) {
        this.pwdSuccessMessage = result.message || 'Password changed successfully!';
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmNewPassword = '';
      } else {
        this.pwdErrorMessage = result.error || 'Failed to change password';
      }
    });
  }

  getPlanBadge(plan: string): string {
    switch (plan) {
      case 'Enterprise': return '🏢 ENTERPRISE';
      case 'Premium': return '💎 PREMIUM';
      case 'Pro': return '⚡ PRO';
      default: return '🆓 FREE';
    }
  }

  // Password policy
  get cpHasMinLength(): boolean { return this.newPassword.length >= 8; }
  get cpHasLowercase(): boolean { return /[a-z]/.test(this.newPassword); }
  get cpHasUppercase(): boolean { return /[A-Z]/.test(this.newPassword); }
  get cpHasDigit(): boolean { return /[0-9]/.test(this.newPassword); }
  get cpHasSpecial(): boolean { return /[!@#$%^&*()_+\-=\[\]{}|;':",./<>?`~]/.test(this.newPassword); }
  get cpNoRepeatingChars(): boolean { return !/(.)\1\1/.test(this.newPassword); }
  get cpAllRulesPass(): boolean {
    return this.cpHasMinLength && this.cpHasLowercase && this.cpHasUppercase &&
           this.cpHasDigit && this.cpHasSpecial && this.cpNoRepeatingChars;
  }
  get cpCanSubmit(): boolean {
    return !this.isChangingPassword && !!this.currentPassword &&
           this.cpAllRulesPass && this.newPassword === this.confirmNewPassword;
  }

  get isLoggedIn(): boolean { return this.authService.isAuthenticated(); }
  get showLayout(): boolean { return this.isLoggedIn && !this.router.url.includes('/login') && !this.router.url.includes('/register'); }

  // Idle timeout
  private resetIdleTimer(): void {
    this.clearIdleTimer();
    if (!this.authService.isAuthenticated()) return;
    this.idleTimer = setTimeout(() => {
      this.authService.logout();
      this.router.navigate(['/login'], { queryParams: { reason: 'timeout' } });
    }, this.IDLE_TIMEOUT_MS);
  }

  private clearIdleTimer(): void {
    if (this.idleTimer) clearTimeout(this.idleTimer);
    if (this.idleWarningTimer) clearTimeout(this.idleWarningTimer);
  }
}
