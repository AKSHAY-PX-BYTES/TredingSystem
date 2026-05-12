import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiResponse, SubscriptionInfo, PlanTier, UpgradeRequest, UpgradeResponse } from '../models/models';

@Injectable({ providedIn: 'root' })
export class SubscriptionApiService {
  constructor(private http: HttpClient) {}

  getStatus(): Observable<SubscriptionInfo | null> {
    return this.http.get<ApiResponse<SubscriptionInfo>>('/subscription/status').pipe(
      map(r => r.data || null),
      catchError(() => of(null))
    );
  }

  getPlans(): Observable<PlanTier[]> {
    return this.http.get<ApiResponse<PlanTier[]>>('/subscription/plans').pipe(
      map(r => r.data || []),
      catchError(() => of([]))
    );
  }

  upgrade(request: UpgradeRequest): Observable<UpgradeResponse | null> {
    return this.http.post<UpgradeResponse>('/subscription/upgrade', request).pipe(
      catchError(() => of(null))
    );
  }

  cancel(): Observable<UpgradeResponse | null> {
    return this.http.post<UpgradeResponse>('/subscription/cancel', null).pipe(
      catchError(() => of(null))
    );
  }

  checkFeatureAccess(feature: string): Observable<boolean> {
    return this.http.get(`/subscription/check-feature/${feature}`).pipe(
      map(() => true),
      catchError(() => of(false))
    );
  }
}
