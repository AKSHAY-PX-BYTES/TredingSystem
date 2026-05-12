import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiResponse, StockQuote, TechnicalIndicators, StockData } from '../models/models';

@Injectable({ providedIn: 'root' })
export class MarketApiService {
  constructor(private http: HttpClient) {}

  getQuote(symbol: string): Observable<StockQuote | null> {
    return this.http.get<ApiResponse<StockQuote>>(`/market/${symbol}`).pipe(
      map(r => r.data || null),
      catchError(() => of(null))
    );
  }

  getIndicators(symbol: string): Observable<TechnicalIndicators | null> {
    return this.http.get<ApiResponse<TechnicalIndicators>>(`/market/${symbol}/indicators`).pipe(
      map(r => r.data || null),
      catchError(() => of(null))
    );
  }

  getHistory(symbol: string, days: number = 30): Observable<StockData[]> {
    return this.http.get<ApiResponse<StockData[]>>(`/market/${symbol}/history?days=${days}`).pipe(
      map(r => r.data || []),
      catchError(() => of([]))
    );
  }
}
