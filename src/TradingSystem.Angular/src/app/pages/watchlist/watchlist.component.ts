import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { WatchlistService } from '../../services/watchlist.service';
import { CurrencyService } from '../../services/currency.service';
import { WatchlistItem } from '../../models/models';

@Component({
  selector: 'app-watchlist',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="watchlist-page">
      <h1>⭐ My Watchlist</h1>
      <div *ngIf="items.length === 0" class="empty-state"><span class="empty-icon">⭐</span><p>Your watchlist is empty</p><p class="empty-sub">Add stocks from the Explore page</p></div>
      <div class="watchlist-grid" *ngIf="items.length > 0">
        <div *ngFor="let item of items" class="watchlist-card">
          <div class="wl-header">
            <a [routerLink]="'/charts/' + item.symbol" class="wl-symbol">{{item.symbol}}</a>
            <button class="wl-remove" (click)="remove(item.symbol)" title="Remove">✕</button>
          </div>
          <div class="wl-name">{{item.companyName}}</div>
          <div class="wl-price" *ngIf="item.currentPrice">{{currencyService.formatPrice(item.currentPrice)}}</div>
        </div>
      </div>
    </div>
  `
})
export class WatchlistComponent implements OnInit {
  items: WatchlistItem[] = [];

  constructor(public watchlistService: WatchlistService, public currencyService: CurrencyService) {}

  ngOnInit(): void {
    this.watchlistService.watchlist$.subscribe(list => this.items = list);
  }

  remove(symbol: string): void {
    this.watchlistService.removeFromWatchlist(symbol);
  }
}
