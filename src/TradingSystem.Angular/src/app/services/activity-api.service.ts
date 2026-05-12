import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiResponse, ActivityStatsModel, ActivityTimelineModel, CountryStatsModel, DeviceStatsModel, ActivityLogModel } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ActivityApiService {
  constructor(private http: HttpClient) {}

  getStats(days: number = 30): Observable<ActivityStatsModel | null> {
    return this.http.get<ApiResponse<ActivityStatsModel>>(`/admin/activity/stats?days=${days}`).pipe(
      map(r => r.data || null), catchError(() => of(null))
    );
  }

  getTimeline(days: number = 30): Observable<ActivityTimelineModel[]> {
    return this.http.get<ApiResponse<ActivityTimelineModel[]>>(`/admin/activity/timeline?days=${days}`).pipe(
      map(r => r.data || []), catchError(() => of([]))
    );
  }

  getCountries(days: number = 30): Observable<CountryStatsModel[]> {
    return this.http.get<ApiResponse<CountryStatsModel[]>>(`/admin/activity/countries?days=${days}`).pipe(
      map(r => r.data || []), catchError(() => of([]))
    );
  }

  getDevices(days: number = 30): Observable<DeviceStatsModel[]> {
    return this.http.get<ApiResponse<DeviceStatsModel[]>>(`/admin/activity/devices?days=${days}`).pipe(
      map(r => r.data || []), catchError(() => of([]))
    );
  }

  getRecent(count: number = 50): Observable<ActivityLogModel[]> {
    return this.http.get<ApiResponse<ActivityLogModel[]>>(`/admin/activity/recent?count=${count}`).pipe(
      map(r => r.data || []), catchError(() => of([]))
    );
  }

  search(eventType?: string, username?: string, country?: string, page: number = 1): Observable<ActivityLogModel[]> {
    let query = `/admin/activity/search?page=${page}`;
    if (eventType) query += `&eventType=${eventType}`;
    if (username) query += `&username=${username}`;
    if (country) query += `&country=${country}`;
    return this.http.get<ApiResponse<ActivityLogModel[]>>(query).pipe(
      map(r => r.data || []), catchError(() => of([]))
    );
  }
}
