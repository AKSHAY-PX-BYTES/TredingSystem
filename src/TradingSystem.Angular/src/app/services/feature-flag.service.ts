import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { FeatureFlagItem } from '../models/models';

@Injectable({ providedIn: 'root' })
export class FeatureFlagService {
  private flagsSubject = new BehaviorSubject<FeatureFlagItem[]>([]);
  private loaded = false;

  flags$ = this.flagsSubject.asObservable();

  constructor(private http: HttpClient) {}

  loadFlags(): Observable<FeatureFlagItem[]> {
    return this.http.get<{ success: boolean; data: FeatureFlagItem[] }>('/admin/features').pipe(
      map(r => r.data || []),
      tap(flags => { this.flagsSubject.next(flags); this.loaded = true; }),
      catchError(() => { this.loaded = false; return of([]); })
    );
  }

  isEnabled(featureKey: string): boolean {
    if (!this.loaded) return true;
    const flag = this.flagsSubject.value.find(f => f.featureKey === featureKey);
    return flag?.isEnabled ?? true;
  }

  getAllFlags(): FeatureFlagItem[] {
    return this.flagsSubject.value;
  }

  updateFlag(featureKey: string, isEnabled: boolean): Observable<boolean> {
    return this.http.put(`/admin/features/${featureKey}`, { isEnabled }).pipe(
      map(() => {
        const flags = this.flagsSubject.value;
        const flag = flags.find(f => f.featureKey === featureKey);
        if (flag) { flag.isEnabled = isEnabled; this.flagsSubject.next([...flags]); }
        return true;
      }),
      catchError(() => of(false))
    );
  }
}
