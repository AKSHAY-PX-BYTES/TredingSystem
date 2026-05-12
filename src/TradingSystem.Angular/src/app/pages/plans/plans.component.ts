import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SubscriptionApiService } from '../../services/subscription-api.service';
import { AuthService } from '../../core/services/auth.service';
import { PlanTier, SubscriptionInfo } from '../../models/models';

@Component({
  selector: 'app-plans',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="plans-page">
      <h1>🏷️ Subscription Plans</h1>
      <p *ngIf="currentSub" class="current-plan-info">Current Plan: <strong>{{currentSub.plan}}</strong> <span *ngIf="currentSub.isTrialActive">(Trial: {{currentSub.daysRemaining}} days left)</span></p>
      <div class="plans-grid">
        <div *ngFor="let plan of plans" class="plan-card" [class.plan-popular]="plan.isPopular" [class.plan-current]="currentSub?.plan === plan.name">
          <div class="plan-badge-popular" *ngIf="plan.isPopular">Most Popular</div>
          <h3>{{plan.name}}</h3>
          <div class="plan-price"><span class="plan-amount">\${{plan.monthlyPrice}}</span>/mo</div>
          <ul class="plan-features"><li *ngFor="let f of plan.features">✓ {{f}}</li></ul>
          <button class="btn-primary" (click)="upgrade(plan.name)" [disabled]="isUpgrading || currentSub?.plan === plan.name">
            {{currentSub?.plan === plan.name ? 'Current Plan' : 'Upgrade'}}
          </button>
        </div>
      </div>
    </div>
  `
})
export class PlansComponent implements OnInit {
  plans: PlanTier[] = [];
  currentSub: SubscriptionInfo | null = null;
  isUpgrading = false;

  constructor(private subApi: SubscriptionApiService, private authService: AuthService) {}

  ngOnInit(): void {
    this.subApi.getPlans().subscribe(p => this.plans = p);
    this.subApi.getStatus().subscribe(s => this.currentSub = s);
  }

  upgrade(plan: string): void {
    this.isUpgrading = true;
    this.subApi.upgrade({ plan }).subscribe(r => {
      this.isUpgrading = false;
      if (r?.success) {
        this.subApi.getStatus().subscribe(s => this.currentSub = s);
        this.authService.refreshToken().subscribe();
      }
    });
  }
}
