import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FeatureFlagService } from '../../services/feature-flag.service';
import { ActivityApiService } from '../../services/activity-api.service';
import { FeatureFlagItem, ActivityStatsModel, ActivityLogModel } from '../../models/models';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="admin-page">
      <h1>⚙️ Admin Dashboard</h1>
      <div class="admin-tabs">
        <button [class.active]="tab==='features'" (click)="tab='features'">Feature Flags</button>
        <button [class.active]="tab==='activity'" (click)="tab='activity'">Activity Logs</button>
      </div>
      <!-- Feature Flags -->
      <div *ngIf="tab==='features'" class="admin-section">
        <h2>Feature Flags</h2>
        <div *ngFor="let flag of flags" class="feature-flag-row">
          <div><strong>{{flag.displayName}}</strong><p>{{flag.description}}</p></div>
          <label class="toggle"><input type="checkbox" [checked]="flag.isEnabled" (change)="toggleFlag(flag)" /><span class="toggle-slider"></span></label>
        </div>
      </div>
      <!-- Activity -->
      <div *ngIf="tab==='activity'" class="admin-section">
        <h2>Activity Dashboard</h2>
        <div *ngIf="stats" class="admin-stats">
          <div class="stat-card"><span>Total Logins</span><strong>{{stats.totalLogins}}</strong></div>
          <div class="stat-card"><span>Unique Users</span><strong>{{stats.uniqueUsers}}</strong></div>
          <div class="stat-card"><span>Total Events</span><strong>{{stats.totalEvents}}</strong></div>
        </div>
        <h3>Recent Activity</h3>
        <table class="admin-table">
          <thead><tr><th>User</th><th>Event</th><th>IP</th><th>Time</th></tr></thead>
          <tbody><tr *ngFor="let log of recentLogs"><td>{{log.username}}</td><td>{{log.eventType}}</td><td>{{log.ipAddress}}</td><td>{{log.createdAt | date:'short'}}</td></tr></tbody>
        </table>
      </div>
    </div>
  `
})
export class AdminComponent implements OnInit {
  tab = 'features';
  flags: FeatureFlagItem[] = [];
  stats: ActivityStatsModel | null = null;
  recentLogs: ActivityLogModel[] = [];

  constructor(private featureFlagService: FeatureFlagService, private activityApi: ActivityApiService) {}

  ngOnInit(): void {
    this.featureFlagService.loadFlags().subscribe(f => this.flags = f);
    this.activityApi.getStats().subscribe(s => this.stats = s);
    this.activityApi.getRecent(20).subscribe(l => this.recentLogs = l);
  }

  toggleFlag(flag: FeatureFlagItem): void {
    this.featureFlagService.updateFlag(flag.featureKey, !flag.isEnabled).subscribe();
  }
}
