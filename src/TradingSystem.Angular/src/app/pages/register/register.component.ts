import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { RegisterRequest } from '../../models/models';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './register.component.html'
})
export class RegisterComponent {
  model: RegisterRequest = { username: '', email: '', password: '', confirmPassword: '', plan: 'Free' };
  errorMessage = '';
  successMessage = '';
  isLoading = false;
  showPassword = false;

  constructor(private authService: AuthService, private router: Router) {}

  handleRegister(): void {
    if (!this.model.username || !this.model.email || !this.model.password) {
      this.errorMessage = 'All fields are required.';
      return;
    }
    if (this.model.password !== this.model.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }
    this.isLoading = true;
    this.errorMessage = '';

    this.authService.register(this.model).subscribe(result => {
      this.isLoading = false;
      if (result.success) {
        this.successMessage = result.message || 'Registration successful! Please login.';
        setTimeout(() => this.router.navigate(['/login']), 2000);
      } else {
        this.errorMessage = result.error || 'Registration failed.';
      }
    });
  }
}
