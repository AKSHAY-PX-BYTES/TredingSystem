import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { tap, catchError, map } from 'rxjs/operators';
import {
  LoginRequest, LoginResponse, RegisterRequest, RegisterResponse,
  UserInfo, SendOtpResponse, VerifyOtpResponse,
  ChangePasswordRequest, ChangePasswordResponse
} from '../../models/models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'authToken';
  private readonly USER_KEY = 'authUser';

  private userSubject = new BehaviorSubject<UserInfo | null>(this.getStoredUser());
  user$ = this.userSubject.asObservable();

  constructor(private http: HttpClient) {}

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/auth/login', request).pipe(
      tap(result => {
        if (result.success && result.token) {
          localStorage.setItem(this.TOKEN_KEY, result.token);
          localStorage.setItem(this.USER_KEY, JSON.stringify(result.user));
          this.userSubject.next(result.user || null);
        }
      }),
      catchError(err => of({ success: false, error: `Connection failed: ${err.message}` } as LoginResponse))
    );
  }

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>('/auth/register', request).pipe(
      catchError(err => of({ success: false, error: `Connection failed: ${err.message}` } as RegisterResponse))
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.userSubject.next(null);
  }

  refreshToken(): Observable<boolean> {
    const username = this.getUsername();
    if (!username) return of(false);
    return this.http.post<LoginResponse>('/auth/refresh', { username }).pipe(
      map(result => {
        if (result?.success && result.token) {
          localStorage.setItem(this.TOKEN_KEY, result.token);
          localStorage.setItem(this.USER_KEY, JSON.stringify(result.user));
          this.userSubject.next(result.user || null);
          return true;
        }
        return false;
      }),
      catchError(() => of(false))
    );
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getUsername(): string | null {
    const user = this.getStoredUser();
    return user?.username || null;
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getUser(): UserInfo | null {
    return this.userSubject.value;
  }

  sendOtp(email: string): Observable<SendOtpResponse> {
    return this.http.post<SendOtpResponse>('/auth/send-otp', { email }).pipe(
      catchError(err => of({ success: false, error: `Connection failed: ${err.message}` } as SendOtpResponse))
    );
  }

  verifyOtp(email: string, code: string): Observable<VerifyOtpResponse> {
    return this.http.post<VerifyOtpResponse>('/auth/verify-otp', { email, code }).pipe(
      catchError(err => of({ success: false, error: `Connection failed: ${err.message}` } as VerifyOtpResponse))
    );
  }

  changePassword(request: ChangePasswordRequest): Observable<ChangePasswordResponse> {
    return this.http.post<ChangePasswordResponse>('/auth/change-password', request).pipe(
      catchError(err => of({ success: false, error: `Connection failed: ${err.message}` } as ChangePasswordResponse))
    );
  }

  checkUsernameExists(username: string): Observable<boolean> {
    return this.http.get<{ exists: boolean }>(`/auth/check-username/${encodeURIComponent(username)}`).pipe(
      map(r => r?.exists ?? false),
      catchError(() => of(false))
    );
  }

  sendPhoneOtp(phoneNumber: string, countryCode: string): Observable<SendOtpResponse> {
    return this.http.post<SendOtpResponse>('/auth/send-phone-otp', { phoneNumber, countryCode }).pipe(
      catchError(err => of({ success: false, error: err.message } as SendOtpResponse))
    );
  }

  verifyPhoneOtp(phoneNumber: string, countryCode: string, code: string): Observable<VerifyOtpResponse> {
    return this.http.post<VerifyOtpResponse>('/auth/verify-phone-otp', { phoneNumber, countryCode, code }).pipe(
      catchError(err => of({ success: false, error: err.message } as VerifyOtpResponse))
    );
  }

  private getStoredUser(): UserInfo | null {
    try {
      const json = localStorage.getItem(this.USER_KEY);
      return json ? JSON.parse(json) : null;
    } catch {
      return null;
    }
  }
}
