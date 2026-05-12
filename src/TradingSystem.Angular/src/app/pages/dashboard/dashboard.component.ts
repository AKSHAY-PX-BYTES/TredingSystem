import { Component, OnInit, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MarketApiService } from '../../services/market-api.service';
import { PredictionApiService } from '../../services/prediction-api.service';
import { WatchlistService } from '../../services/watchlist.service';
import { CurrencyService } from '../../services/currency.service';
import { FeatureFlagService } from '../../services/feature-flag.service';
import { StockQuote, StrategyResult } from '../../models/models';

declare const Chart: any;

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit, AfterViewInit {
  @ViewChild('priceChart') chartRef!: ElementRef;

  symbolInput = '';
  currentSymbol = 'AAPL';
  popularSymbols = ['AAPL', 'MSFT', 'GOOGL', 'AMZN', 'TSLA', 'RELIANCE.NS', 'TCS.NS'];
  quote: StockQuote | null = null;
  strategy: StrategyResult | null = null;
  isLoading = false;
  error = '';
  isInWatchlist = false;
  exploreEnabled = true;
  private chartInstance: any;

  constructor(
    private marketApi: MarketApiService,
    private predictionApi: PredictionApiService,
    public watchlistService: WatchlistService,
    public currencyService: CurrencyService,
    private featureFlagService: FeatureFlagService
  ) {}

  ngOnInit(): void {
    this.exploreEnabled = this.featureFlagService.isEnabled('explore_search');
    this.loadData();
  }

  ngAfterViewInit(): void {}

  selectSymbol(symbol: string): void {
    this.currentSymbol = symbol;
    this.symbolInput = symbol;
    this.loadData();
  }

  handleKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter') this.loadData();
  }

  loadData(): void {
    const symbol = this.symbolInput.trim() || this.currentSymbol;
    this.currentSymbol = symbol.toUpperCase();
    this.isLoading = true;
    this.error = '';
    this.strategy = null;

    this.marketApi.getQuote(this.currentSymbol).subscribe(q => {
      this.isLoading = false;
      if (q) {
        this.quote = q;
        this.isInWatchlist = this.watchlistService.isInWatchlist(this.currentSymbol);
        this.loadStrategy();
        this.loadChart();
      } else {
        this.error = `Unable to load data for ${this.currentSymbol}`;
      }
    });
  }

  private loadStrategy(): void {
    this.predictionApi.getStrategy(this.currentSymbol).subscribe(s => {
      this.strategy = s;
    });
  }

  private loadChart(): void {
    this.marketApi.getHistory(this.currentSymbol, 30).subscribe(data => {
      if (data.length && typeof Chart !== 'undefined') {
        const ctx = document.getElementById('priceChart') as HTMLCanvasElement;
        if (!ctx) return;
        if (this.chartInstance) this.chartInstance.destroy();
        this.chartInstance = new Chart(ctx.getContext('2d'), {
          type: 'line',
          data: {
            labels: data.map(d => new Date(d.date).toLocaleDateString('en', { month: 'short', day: 'numeric' })),
            datasets: [{
              label: this.currentSymbol,
              data: data.map(d => d.close),
              borderColor: '#00d09c',
              backgroundColor: 'rgba(0,208,156,0.1)',
              tension: 0.4,
              fill: true
            }]
          },
          options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } }
        });
      }
    });
  }

  toggleWatchlist(): void {
    if (this.isInWatchlist) {
      this.watchlistService.removeFromWatchlist(this.currentSymbol);
    } else {
      this.watchlistService.addToWatchlist(this.currentSymbol, this.quote?.companyName || this.currentSymbol);
    }
    this.isInWatchlist = !this.isInWatchlist;
  }

  getCleanSymbol(symbol: string): string {
    return symbol.replace('.NS', '').replace('.BO', '');
  }

  formatVolume(vol: number): string {
    if (vol >= 1e9) return (vol / 1e9).toFixed(1) + 'B';
    if (vol >= 1e6) return (vol / 1e6).toFixed(1) + 'M';
    if (vol >= 1e3) return (vol / 1e3).toFixed(1) + 'K';
    return vol.toString();
  }

  getSignalClass(signal: string): string {
    if (signal.includes('Buy')) return 'signal-buy';
    if (signal.includes('Sell')) return 'signal-sell';
    return 'signal-hold';
  }

  getConfidenceClass(confidence: number): string {
    if (confidence >= 0.7) return 'confidence-high';
    if (confidence >= 0.4) return 'confidence-medium';
    return 'confidence-low';
  }

  getTrendClass(trend: string): string {
    if (trend === 'Bullish') return 'ind-bullish';
    if (trend === 'Bearish') return 'ind-bearish';
    return 'ind-neutral';
  }
}
