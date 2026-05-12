import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiResponse, PredictionResult, StrategyResult } from '../models/models';

@Injectable({ providedIn: 'root' })
export class PredictionApiService {
  constructor(private http: HttpClient) {}

  predict(symbol: string): Observable<PredictionResult | null> {
    return this.http.get<ApiResponse<PredictionResult>>(`/predict/${symbol}`).pipe(
      map(r => r.data || null),
      catchError(() => of(null))
    );
  }

  getStrategy(symbol: string): Observable<StrategyResult | null> {
    return this.http.get<ApiResponse<StrategyResult>>(`/strategy/${symbol}`).pipe(
      map(r => r.data || null),
      catchError(() => of(null))
    );
  }
}
