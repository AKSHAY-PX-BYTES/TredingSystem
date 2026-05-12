import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MarketApiService } from '../../services/market-api.service';
import { CurrencyService } from '../../services/currency.service';
import { StockQuote } from '../../models/models';

@Component({
  selector: 'app-compare',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="compare-page">
      <h1>⚖️ Compare Stocks</h1>
      <div class="compare-inputs">
        <input [(ngModel)]="symbol1" placeholder="Symbol 1 (e.g. AAPL)" class="form-input" />
        <input [(ngModel)]="symbol2" placeholder="Symbol 2 (e.g. MSFT)" class="form-input" />
        <button class="btn-primary" (click)="compare()" [disabled]="isLoading">Compare</button>
      </div>
      <div *ngIf="isLoading" class="loading-container"><div class="loading-spinner"></div></div>
      <div class="compare-grid" *ngIf="quote1 && quote2">
        <div class="compare-card"><h3>{{quote1.symbol}}</h3><p>{{quote1.companyName}}</p><div class="compare-price">{{cs.formatPrice(quote1.currentPrice, quote1.priceCurrency)}}</div><div [class.chg-up]="quote1.changePercent>=0" [class.chg-down]="quote1.changePercent<0">{{quote1.changePercent.toFixed(2)}}%</div></div>
        <div class="compare-vs">VS</div>
        <div class="compare-card"><h3>{{quote2.symbol}}</h3><p>{{quote2.companyName}}</p><div class="compare-price">{{cs.formatPrice(quote2.currentPrice, quote2.priceCurrency)}}</div><div [class.chg-up]="quote2.changePercent>=0" [class.chg-down]="quote2.changePercent<0">{{quote2.changePercent.toFixed(2)}}%</div></div>
      </div>
    </div>
  `
})
export class CompareComponent {
  symbol1 = 'AAPL'; symbol2 = 'MSFT';
  quote1: StockQuote | null = null;
  quote2: StockQuote | null = null;
  isLoading = false;

  constructor(private marketApi: MarketApiService, public cs: CurrencyService) {}

  compare(): void {
    this.isLoading = true;
    this.marketApi.getQuote(this.symbol1.toUpperCase()).subscribe(q => { this.quote1 = q; this.checkDone(); });
    this.marketApi.getQuote(this.symbol2.toUpperCase()).subscribe(q => { this.quote2 = q; this.checkDone(); });
  }

  private checkDone(): void {
    if (this.quote1 !== null || this.quote2 !== null) this.isLoading = false;
  }
}
