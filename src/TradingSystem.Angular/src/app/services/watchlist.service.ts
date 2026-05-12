import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { WatchlistItem } from '../models/models';

@Injectable({ providedIn: 'root' })
export class WatchlistService {
  private readonly STORAGE_KEY = 'trading_watchlist';
  private watchlistSubject = new BehaviorSubject<WatchlistItem[]>(this.loadFromStorage());

  watchlist$ = this.watchlistSubject.asObservable();

  get watchlist(): WatchlistItem[] { return this.watchlistSubject.value; }

  addToWatchlist(symbol: string, companyName: string): void {
    const list = this.watchlistSubject.value;
    if (list.some(w => w.symbol.toLowerCase() === symbol.toLowerCase())) return;
    list.push({
      symbol: symbol.toUpperCase(),
      companyName,
      currentPrice: 0,
      previousPrice: 0,
      addedAt: new Date().toISOString(),
      lastUpdated: new Date().toISOString()
    });
    this.save(list);
  }

  removeFromWatchlist(symbol: string): void {
    const list = this.watchlistSubject.value.filter(
      w => w.symbol.toLowerCase() !== symbol.toLowerCase()
    );
    this.save(list);
  }

  isInWatchlist(symbol: string): boolean {
    return this.watchlistSubject.value.some(
      w => w.symbol.toLowerCase() === symbol.toLowerCase()
    );
  }

  updatePrices(prices: { [symbol: string]: number }): void {
    const list = this.watchlistSubject.value;
    for (const item of list) {
      if (prices[item.symbol] !== undefined) {
        if (item.currentPrice > 0) item.previousPrice = item.currentPrice;
        item.currentPrice = prices[item.symbol];
        item.lastUpdated = new Date().toISOString();
      }
    }
    this.watchlistSubject.next([...list]);
  }

  private save(list: WatchlistItem[]): void {
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(list));
    this.watchlistSubject.next(list);
  }

  private loadFromStorage(): WatchlistItem[] {
    try {
      const json = localStorage.getItem(this.STORAGE_KEY);
      return json ? JSON.parse(json) : [];
    } catch { return []; }
  }
}
