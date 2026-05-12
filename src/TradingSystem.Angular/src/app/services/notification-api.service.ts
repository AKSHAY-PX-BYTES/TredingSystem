import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiResponse, NotificationDto, PriceAlertDto, CreatePriceAlertRequest } from '../models/models';

@Injectable({ providedIn: 'root' })
export class NotificationApiService {
  constructor(private http: HttpClient) {}

  getNotifications(page: number = 1): Observable<NotificationDto[]> {
    return this.http.get<ApiResponse<NotificationDto[]>>(`/notifications?page=${page}`).pipe(
      map(r => r.data || []),
      catchError(() => of([]))
    );
  }

  getUnreadCount(): Observable<number> {
    return this.http.get<{ count: number }>('/notifications/unread-count').pipe(
      map(r => r?.count || 0),
      catchError(() => of(0))
    );
  }

  markAsRead(id: number): Observable<void> {
    return this.http.post<void>(`/notifications/${id}/read`, null).pipe(catchError(() => of(undefined)));
  }

  markAllAsRead(): Observable<void> {
    return this.http.post<void>('/notifications/read-all', null).pipe(catchError(() => of(undefined)));
  }

  getAlerts(): Observable<PriceAlertDto[]> {
    return this.http.get<ApiResponse<PriceAlertDto[]>>('/notifications/alerts').pipe(
      map(r => r.data || []),
      catchError(() => of([]))
    );
  }

  createAlert(request: CreatePriceAlertRequest): Observable<PriceAlertDto | null> {
    return this.http.post<ApiResponse<PriceAlertDto>>('/notifications/alerts', request).pipe(
      map(r => r.data || null),
      catchError(() => of(null))
    );
  }

  deleteAlert(id: number): Observable<void> {
    return this.http.delete<void>(`/notifications/alerts/${id}`).pipe(catchError(() => of(undefined)));
  }
}
