import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiResponse, BacktestRequest, BacktestResult } from '../models/models';

@Injectable({ providedIn: 'root' })
export class BacktestApiService {
  constructor(private http: HttpClient) {}

  run(request: BacktestRequest): Observable<BacktestResult | null> {
    return this.http.post<ApiResponse<BacktestResult>>('/backtest', request).pipe(
      map(r => r.data || null),
      catchError(() => of(null))
    );
  }
}
