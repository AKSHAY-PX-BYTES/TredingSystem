import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { loginGuard } from './core/guards/login.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent),
    canActivate: [loginGuard]
  },
  {
    path: 'register',
    loadComponent: () => import('./pages/register/register.component').then(m => m.RegisterComponent),
    canActivate: [loginGuard]
  },
  {
    path: 'trial-expired',
    loadComponent: () => import('./pages/trial-expired/trial-expired.component').then(m => m.TrialExpiredComponent),
    canActivate: [authGuard]
  },
  {
    path: '',
    loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: 'markets',
    loadComponent: () => import('./pages/markets/markets.component').then(m => m.MarketsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'watchlist',
    loadComponent: () => import('./pages/watchlist/watchlist.component').then(m => m.WatchlistComponent),
    canActivate: [authGuard]
  },
  {
    path: 'charts',
    loadComponent: () => import('./pages/charts/charts.component').then(m => m.ChartsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'charts/:symbol',
    loadComponent: () => import('./pages/charts/charts.component').then(m => m.ChartsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'compare',
    loadComponent: () => import('./pages/compare/compare.component').then(m => m.CompareComponent),
    canActivate: [authGuard]
  },
  {
    path: 'news',
    loadComponent: () => import('./pages/news/news.component').then(m => m.NewsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'backtest',
    loadComponent: () => import('./pages/backtest/backtest.component').then(m => m.BacktestComponent),
    canActivate: [authGuard]
  },
  {
    path: 'plans',
    loadComponent: () => import('./pages/plans/plans.component').then(m => m.PlansComponent),
    canActivate: [authGuard]
  },
  {
    path: 'notifications',
    loadComponent: () => import('./pages/notifications/notifications.component').then(m => m.NotificationsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'signals',
    loadComponent: () => import('./pages/signals/signals.component').then(m => m.SignalsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'chat',
    loadComponent: () => import('./pages/chat/chat.component').then(m => m.ChatComponent),
    canActivate: [authGuard]
  },
  {
    path: 'admin',
    loadComponent: () => import('./pages/admin/admin.component').then(m => m.AdminComponent),
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: '' }
];
