import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NewsApiService } from '../../services/news-api.service';
import { NewsItem, NewsAnalysisResponse } from '../../models/models';

@Component({
  selector: 'app-news',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="news-page">
      <h1>📰 News & Sentiment Analysis</h1>
      <div class="news-controls">
        <input [(ngModel)]="symbol" placeholder="Symbol (e.g. AAPL)" class="form-input" />
        <button class="btn-primary" (click)="loadNews()" [disabled]="isLoading">Analyze</button>
      </div>
      <div *ngIf="isLoading" class="loading-container"><div class="loading-spinner"></div></div>
      <div *ngIf="analysis" class="sentiment-overview">
        <h3>Overall Sentiment: <span [class.chg-up]="analysis.overallSentiment==='Bullish'" [class.chg-down]="analysis.overallSentiment==='Bearish'">{{analysis.overallSentiment}}</span></h3>
        <p>Average Score: {{analysis.averageScore.toFixed(2)}}</p>
      </div>
      <div class="news-list">
        <div *ngFor="let item of news" class="news-card">
          <div class="news-headline">{{item.headline}}</div>
          <div class="news-meta"><span>{{item.source}}</span> • <span>{{item.publishedAt | date:'short'}}</span></div>
        </div>
      </div>
    </div>
  `
})
export class NewsComponent {
  symbol = 'AAPL';
  news: NewsItem[] = [];
  analysis: NewsAnalysisResponse | null = null;
  isLoading = false;

  constructor(private newsApi: NewsApiService) {}

  loadNews(): void {
    this.isLoading = true;
    this.newsApi.getNews(this.symbol.toUpperCase()).subscribe(items => {
      this.news = items;
      if (items.length) {
        this.newsApi.analyze({ headlines: items.map(i => i.headline), symbol: this.symbol }).subscribe(r => {
          this.analysis = r;
          this.isLoading = false;
        });
      } else { this.isLoading = false; }
    });
  }
}
