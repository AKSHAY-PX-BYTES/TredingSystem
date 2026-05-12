import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiResponse, NewsItem, NewsAnalysisRequest, NewsAnalysisResponse } from '../models/models';

@Injectable({ providedIn: 'root' })
export class NewsApiService {
  constructor(private http: HttpClient) {}

  getNews(symbol: string): Observable<NewsItem[]> {
    return this.http.get<ApiResponse<NewsItem[]>>(`/news/${symbol}`).pipe(
      map(r => r.data || []),
      catchError(() => of([]))
    );
  }

  analyze(request: NewsAnalysisRequest): Observable<NewsAnalysisResponse | null> {
    return this.http.post<ApiResponse<NewsAnalysisResponse>>('/news/analyze', request).pipe(
      map(r => r.data || null),
      catchError(() => of(null))
    );
  }
}
