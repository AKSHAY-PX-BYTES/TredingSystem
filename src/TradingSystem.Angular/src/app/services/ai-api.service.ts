import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiResponse, AiSignalDto, ChatRequest, ChatResponse } from '../models/models';

@Injectable({ providedIn: 'root' })
export class AiApiService {
  constructor(private http: HttpClient) {}

  getSignals(symbol?: string): Observable<AiSignalDto[]> {
    const url = symbol ? `/ai/signals?symbol=${symbol}` : '/ai/signals';
    return this.http.get<ApiResponse<AiSignalDto[]>>(url).pipe(
      map(r => r.data || []),
      catchError(() => of([]))
    );
  }

  generateSignal(symbol: string): Observable<AiSignalDto | null> {
    return this.http.post<ApiResponse<AiSignalDto>>(`/ai/signals/generate/${symbol}`, null).pipe(
      map(r => r.data || null),
      catchError(() => of(null))
    );
  }

  sendChat(request: ChatRequest): Observable<ChatResponse | null> {
    return this.http.post<ApiResponse<ChatResponse>>('/ai/chat', request).pipe(
      map(r => r.data || null),
      catchError(() => of(null))
    );
  }
}
