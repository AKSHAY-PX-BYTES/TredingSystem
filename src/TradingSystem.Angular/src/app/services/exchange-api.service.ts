import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiResponse, ExchangeData } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ExchangeApiService {
  constructor(private http: HttpClient) {}

  getExchangeData(exchange: string): Observable<ExchangeData | null> {
    return this.http.get<ApiResponse<ExchangeData>>(`/exchange/${exchange}`).pipe(
      map(r => r.data || null),
      catchError(() => of(null))
    );
  }
}
