import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BacktestApiService } from '../../services/backtest-api.service';
import { CurrencyService } from '../../services/currency.service';
import { BacktestRequest, BacktestResult } from '../../models/models';

declare const Chart: any;

@Component({
  selector: 'app-backtest',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="backtest-page">
      <h1>🔄 Strategy Backtesting</h1>
      <div class="backtest-form">
        <input [(ngModel)]="request.symbol" placeholder="Symbol" class="form-input" />
        <select [(ngModel)]="request.strategy" class="form-input"><option value="Combined">Combined</option><option value="SMA_Crossover">SMA Crossover</option><option value="RSI_MACD">RSI + MACD</option></select>
        <input [(ngModel)]="request.days" type="number" placeholder="Days" class="form-input" />
        <input [(ngModel)]="request.initialCapital" type="number" placeholder="Capital" class="form-input" />
        <button class="btn-primary" (click)="run()" [disabled]="isLoading">{{isLoading ? 'Running...' : 'Run Backtest'}}</button>
      </div>
      <div *ngIf="isLoading" class="loading-container"><div class="loading-spinner"></div></div>
      <div *ngIf="result" class="backtest-results">
        <div class="backtest-stats">
          <div class="stat-card"><span>Total Return</span><strong [class.chg-up]="result.totalReturnPercent>=0" [class.chg-down]="result.totalReturnPercent<0">{{result.totalReturnPercent.toFixed(2)}}%</strong></div>
          <div class="stat-card"><span>Win Rate</span><strong>{{result.winRate.toFixed(1)}}%</strong></div>
          <div class="stat-card"><span>Trades</span><strong>{{result.totalTrades}}</strong></div>
          <div class="stat-card"><span>Sharpe Ratio</span><strong>{{result.sharpeRatio.toFixed(2)}}</strong></div>
          <div class="stat-card"><span>Max Drawdown</span><strong class="chg-down">{{result.maxDrawdown.toFixed(2)}}%</strong></div>
          <div class="stat-card"><span>Final Capital</span><strong>{{cs.formatPrice(result.finalCapital)}}</strong></div>
        </div>
        <div class="chart-container tall"><canvas id="equityChart"></canvas></div>
      </div>
    </div>
  `
})
export class BacktestComponent {
  request: BacktestRequest = { symbol: 'AAPL', days: 30, initialCapital: 10000, strategy: 'Combined' };
  result: BacktestResult | null = null;
  isLoading = false;
  private chartInstance: any;

  constructor(private backtestApi: BacktestApiService, public cs: CurrencyService) {}

  run(): void {
    this.isLoading = true;
    this.result = null;
    this.backtestApi.run({ ...this.request, symbol: this.request.symbol.toUpperCase() }).subscribe(r => {
      this.result = r;
      this.isLoading = false;
      if (r?.equityCurve?.length) setTimeout(() => this.renderChart(), 50);
    });
  }

  private renderChart(): void {
    const ctx = document.getElementById('equityChart') as HTMLCanvasElement;
    if (!ctx || !this.result) return;
    if (this.chartInstance) this.chartInstance.destroy();
    this.chartInstance = new Chart(ctx.getContext('2d'), {
      type: 'line',
      data: {
        labels: this.result.equityCurve.map(p => new Date(p.date).toLocaleDateString('en', { month: 'short', day: 'numeric' })),
        datasets: [{ label: 'Portfolio Value', data: this.result.equityCurve.map(p => p.value), borderColor: '#00d09c', fill: true, backgroundColor: 'rgba(0,208,156,0.1)' }]
      },
      options: { responsive: true, maintainAspectRatio: false }
    });
  }
}
