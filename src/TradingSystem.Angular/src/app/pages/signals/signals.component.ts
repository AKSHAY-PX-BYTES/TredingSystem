import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiApiService } from '../../services/ai-api.service';
import { AiSignalDto } from '../../models/models';

@Component({
  selector: 'app-signals',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="signals-page">
      <div class="page-header"><h1>🤖 AI Trading Signals</h1><p class="page-subtitle">ML-powered signals using RSI, MACD, Volume clustering, Sentiment & more</p></div>
      <div class="signal-sources">
        <button class="source-btn" [class.active]="activeSource===''" (click)="activeSource=''">All</button>
        <button class="source-btn" [class.active]="activeSource==='RSI_MACD'" (click)="activeSource='RSI_MACD'">📊 RSI+MACD</button>
        <button class="source-btn" [class.active]="activeSource==='Sentiment'" (click)="activeSource='Sentiment'">💬 Sentiment</button>
        <button class="source-btn" [class.active]="activeSource==='Earnings'" (click)="activeSource='Earnings'">📈 Earnings</button>
        <button class="source-btn" [class.active]="activeSource==='Insider'" (click)="activeSource='Insider'">🕵️ Insider</button>
        <button class="source-btn" [class.active]="activeSource==='Correlation'" (click)="activeSource='Correlation'">🔗 Correlation</button>
      </div>
      <div class="signal-generate">
        <input [(ngModel)]="symbolInput" placeholder="Enter symbol (e.g. AAPL)" class="form-input" />
        <button class="btn-primary" (click)="generateSignal()" [disabled]="isGenerating">{{isGenerating ? 'Generating...' : '🤖 Generate Signal'}}</button>
      </div>
      <div class="signals-grid">
        <div *ngIf="isLoading" class="loading-state"><div class="loading-spinner"></div> Loading signals...</div>
        <div *ngIf="!isLoading && filteredSignals.length === 0" class="empty-state"><span class="empty-icon">🤖</span><p>No signals yet</p></div>
        <div *ngFor="let signal of filteredSignals" class="signal-card" [class]="'signal-' + signal.signalType.toLowerCase()">
          <div class="signal-header"><span class="signal-symbol">{{signal.symbol}}</span><span class="signal-badge" [class]="'signal-badge-' + signal.signalType.toLowerCase()">{{signal.signalType}}</span></div>
          <div class="signal-confidence"><div class="confidence-bar"><div class="confidence-fill" [style.width.%]="signal.confidence"></div></div><span class="confidence-text">{{signal.confidence}}% confidence</span></div>
          <div class="signal-source"><span class="source-tag">{{signal.source}}</span></div>
          <p class="signal-analysis">{{signal.analysis}}</p>
          <div class="signal-footer"><span class="signal-time">{{signal.generatedAt | date:'MMM dd, HH:mm'}}</span></div>
        </div>
      </div>
    </div>
  `
})
export class SignalsComponent implements OnInit {
  signals: AiSignalDto[] = [];
  activeSource = '';
  symbolInput = '';
  isLoading = true;
  isGenerating = false;

  get filteredSignals(): AiSignalDto[] {
    return this.activeSource ? this.signals.filter(s => s.source === this.activeSource) : this.signals;
  }

  constructor(private aiApi: AiApiService) {}

  ngOnInit(): void {
    this.aiApi.getSignals().subscribe(s => { this.signals = s; this.isLoading = false; });
  }

  generateSignal(): void {
    if (!this.symbolInput.trim()) return;
    this.isGenerating = true;
    this.aiApi.generateSignal(this.symbolInput.trim().toUpperCase()).subscribe(s => {
      if (s) this.signals.unshift(s);
      this.isGenerating = false;
      this.symbolInput = '';
    });
  }
}
