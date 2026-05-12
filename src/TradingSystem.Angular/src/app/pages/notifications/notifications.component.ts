import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NotificationApiService } from '../../services/notification-api.service';
import { NotificationDto, PriceAlertDto, CreatePriceAlertRequest } from '../../models/models';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="notifications-page">
      <div class="page-header"><h1>🔔 Notifications</h1><button class="btn-secondary" (click)="markAllRead()">Mark All Read</button></div>
      <div class="tab-bar"><button [class.active]="tab==='notifications'" (click)="tab='notifications'">Notifications</button><button [class.active]="tab==='alerts'" (click)="tab='alerts'">Price Alerts</button></div>
      <div *ngIf="tab==='notifications'">
        <div *ngIf="notifications.length===0" class="empty-state"><p>No notifications</p></div>
        <div *ngFor="let n of notifications" class="notification-item" [class.unread]="!n.isRead" (click)="markRead(n)">
          <div class="notif-title">{{n.title}}</div><div class="notif-msg">{{n.message}}</div><div class="notif-time">{{n.createdAt | date:'short'}}</div>
        </div>
      </div>
      <div *ngIf="tab==='alerts'">
        <div class="alert-form">
          <input [(ngModel)]="alertReq.symbol" placeholder="Symbol" class="form-input" />
          <input [(ngModel)]="alertReq.targetPrice" type="number" placeholder="Target Price" class="form-input" />
          <select [(ngModel)]="alertReq.direction" class="form-input"><option value="Above">Above</option><option value="Below">Below</option></select>
          <button class="btn-primary" (click)="createAlert()">Create Alert</button>
        </div>
        <div *ngFor="let a of alerts" class="alert-item">
          <span>{{a.symbol}} {{a.direction}} \${{a.targetPrice}}</span>
          <span class="alert-status">{{a.isTriggered ? '✅ Triggered' : a.isActive ? '🟢 Active' : '⏸️ Paused'}}</span>
          <button class="btn-icon" (click)="deleteAlert(a.id)">🗑️</button>
        </div>
      </div>
    </div>
  `
})
export class NotificationsComponent implements OnInit {
  notifications: NotificationDto[] = [];
  alerts: PriceAlertDto[] = [];
  tab = 'notifications';
  alertReq: CreatePriceAlertRequest = { symbol: '', targetPrice: 0, thresholdPercent: 5, direction: 'Above' };

  constructor(private notifApi: NotificationApiService) {}

  ngOnInit(): void {
    this.notifApi.getNotifications().subscribe(n => this.notifications = n);
    this.notifApi.getAlerts().subscribe(a => this.alerts = a);
  }

  markRead(n: NotificationDto): void {
    if (!n.isRead) { this.notifApi.markAsRead(n.id).subscribe(() => n.isRead = true); }
  }

  markAllRead(): void {
    this.notifApi.markAllAsRead().subscribe(() => this.notifications.forEach(n => n.isRead = true));
  }

  createAlert(): void {
    this.notifApi.createAlert(this.alertReq).subscribe(a => { if (a) this.alerts.unshift(a); });
  }

  deleteAlert(id: number): void {
    this.notifApi.deleteAlert(id).subscribe(() => this.alerts = this.alerts.filter(a => a.id !== id));
  }
}
