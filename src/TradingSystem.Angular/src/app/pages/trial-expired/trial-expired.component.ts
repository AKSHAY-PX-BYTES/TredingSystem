import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-trial-expired',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="trial-expired-page">
      <div class="trial-expired-card">
        <div class="trial-icon">⏰</div>
        <h1>Free Trial Expired</h1>
        <p>Your 7-day free trial has ended. Upgrade your plan to continue using the platform.</p>
        <a routerLink="/plans" class="btn-primary">View Plans & Upgrade</a>
      </div>
    </div>
  `
})
export class TrialExpiredComponent {}
