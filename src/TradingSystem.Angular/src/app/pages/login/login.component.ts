import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { FeatureFlagService } from '../../services/feature-flag.service';
import { LoginRequest } from '../../models/models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  loginModel: LoginRequest = { username: '', password: '' };
  errorMessage = '';
  isLoading = false;
  showPassword = false;
  showTimeoutMessage = false;
  showExpiredMessage = false;
  signupEnabled = true;

  constructor(
    private authService: AuthService,
    private featureFlagService: FeatureFlagService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.route.queryParams.subscribe(params => {
      this.showTimeoutMessage = params['reason'] === 'timeout';
      this.showExpiredMessage = params['reason'] === 'expired';
    });
    this.featureFlagService.loadFlags().subscribe(() => {
      this.signupEnabled = this.featureFlagService.isEnabled('signup');
    });
  }

  handleLogin(): void {
    if (!this.loginModel.username || !this.loginModel.password) {
      this.errorMessage = 'Please enter both username and password.';
      return;
    }
    this.isLoading = true;
    this.errorMessage = '';

    this.authService.login(this.loginModel).subscribe(result => {
      this.isLoading = false;
      if (result.success) {
        this.router.navigate(['/']);
      } else {
        this.errorMessage = result.error || 'Login failed. Please check your credentials.';
      }
    });
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }
}
