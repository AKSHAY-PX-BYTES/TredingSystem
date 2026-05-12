import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MarketApiService } from '../../services/market-api.service';
import { CurrencyService } from '../../services/currency.service';
import { StockData } from '../../models/models';

declare const Chart: any;

@Component({
  selector: 'app-charts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="charts-page">
      <h1>📈 Advanced Charts</h1>
      <div class="chart-controls">
        <input [(ngModel)]="symbol" placeholder="Enter symbol" class="form-input" />
        <select [(ngModel)]="days" class="form-input"><option [value]="7">7D</option><option [value]="30">30D</option><option [value]="90">90D</option><option [value]="365">1Y</option></select>
        <button class="btn-primary" (click)="loadChart()">Load Chart</button>
      </div>
      <div *ngIf="isLoading" class="loading-container"><div class="loading-spinner"></div></div>
      <div class="chart-container tall" *ngIf="!isLoading"><canvas id="advancedChart"></canvas></div>
    </div>
  `
})
export class ChartsComponent implements OnInit {
  symbol = 'AAPL';
  days = 30;
  isLoading = false;
  private chartInstance: any;

  constructor(private marketApi: MarketApiService, public currencyService: CurrencyService, private route: ActivatedRoute) {}

  ngOnInit(): void {
    const s = this.route.snapshot.paramMap.get('symbol');
    if (s) this.symbol = s;
    this.loadChart();
  }

  loadChart(): void {
    this.isLoading = true;
    this.marketApi.getHistory(this.symbol.toUpperCase(), this.days).subscribe(data => {
      this.isLoading = false;
      if (data.length && typeof Chart !== 'undefined') {
        setTimeout(() => this.renderChart(data), 50);
      }
    });
  }

  private renderChart(data: StockData[]): void {
    const ctx = document.getElementById('advancedChart') as HTMLCanvasElement;
    if (!ctx) return;
    if (this.chartInstance) this.chartInstance.destroy();
    this.chartInstance = new Chart(ctx.getContext('2d'), {
      type: 'candlestick' in Chart.controllers ? 'candlestick' : 'line',
      data: {
        labels: data.map(d => new Date(d.date).toLocaleDateString('en', { month: 'short', day: 'numeric' })),
        datasets: [{ label: this.symbol, data: data.map(d => d.close), borderColor: '#00d09c', backgroundColor: 'rgba(0,208,156,0.1)', tension: 0.4, fill: true }]
      },
      options: { responsive: true, maintainAspectRatio: false }
    });
  }
}
