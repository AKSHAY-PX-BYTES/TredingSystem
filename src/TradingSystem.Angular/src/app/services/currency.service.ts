import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { ApiResponse, CurrencyInfo, CurrencyListResponse } from '../models/models';

@Injectable({ providedIn: 'root' })
export class CurrencyService {
  private readonly STORAGE_KEY = 'selectedCurrency';
  private currenciesSubject = new BehaviorSubject<CurrencyInfo[]>([]);
  private currentCurrency: CurrencyInfo = { code: 'USD', symbol: '$', name: 'US Dollar', rateFromUsd: 1 };

  currencies$ = this.currenciesSubject.asObservable();

  get currentCode(): string { return this.currentCurrency.code; }
  get currentSymbol(): string { return this.currentCurrency.symbol; }
  get currentRate(): number { return this.currentCurrency.rateFromUsd; }
  get availableCurrencies(): CurrencyInfo[] { return this.currenciesSubject.value; }

  constructor(private http: HttpClient) {}

  loadCurrencies(): Observable<CurrencyInfo[]> {
    return this.http.get<ApiResponse<CurrencyListResponse>>('/currency').pipe(
      map(r => r.data?.currencies || this.getDefaultCurrencies()),
      tap(currencies => {
        this.currenciesSubject.next(currencies);
        const saved = localStorage.getItem(this.STORAGE_KEY);
        if (saved) {
          const found = currencies.find((c: CurrencyInfo) => c.code === saved);
          if (found) this.currentCurrency = found;
        }
      }),
      catchError(() => {
        const defaults = this.getDefaultCurrencies();
        this.currenciesSubject.next(defaults);
        return of(defaults);
      })
    );
  }

  setCurrency(code: string): void {
    const found = this.currenciesSubject.value.find(c => c.code === code);
    if (found) {
      this.currentCurrency = found;
      localStorage.setItem(this.STORAGE_KEY, code);
    }
  }

  formatPrice(usdValue: number, sourceCurrency: string = 'USD'): string {
    const converted = this.convert(usdValue, sourceCurrency);
    return `${this.currentSymbol}${converted.toFixed(2)}`;
  }

  convert(value: number, sourceCurrency: string = 'USD'): number {
    if (sourceCurrency === this.currentCode) return value;
    // Convert to USD first, then to target
    const sourceInfo = this.currenciesSubject.value.find(c => c.code === sourceCurrency);
    const usdValue = sourceInfo ? value / sourceInfo.rateFromUsd : value;
    return usdValue * this.currentRate;
  }

  private getDefaultCurrencies(): CurrencyInfo[] {
    return [
      { code: 'USD', symbol: '$', name: 'US Dollar', rateFromUsd: 1 },
      { code: 'INR', symbol: '₹', name: 'Indian Rupee', rateFromUsd: 83.5 },
      { code: 'EUR', symbol: '€', name: 'Euro', rateFromUsd: 0.92 },
      { code: 'GBP', symbol: '£', name: 'British Pound', rateFromUsd: 0.79 }
    ];
  }
}
