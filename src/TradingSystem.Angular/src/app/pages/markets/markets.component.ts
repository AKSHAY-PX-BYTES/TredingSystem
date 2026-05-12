import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ExchangeApiService } from '../../services/exchange-api.service';
import { CurrencyService } from '../../services/currency.service';
import { ExchangeData } from '../../models/models';

@Component({
  selector: 'app-markets',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './markets.component.html'
})
export class MarketsComponent implements OnInit {
  exchanges = ['NSE', 'BSE', 'NYSE', 'NASDAQ', 'LSE', 'HKEX', 'SGX', 'BINANCE'];
  selectedExchange = 'NSE';
  exchangeData: ExchangeData | null = null;
  isLoading = false;
  activeTab = 'overview';

  constructor(private exchangeApi: ExchangeApiService, public currencyService: CurrencyService) {}

  ngOnInit(): void { this.loadExchange(); }

  selectExchange(code: string): void {
    this.selectedExchange = code;
    this.loadExchange();
  }

  loadExchange(): void {
    this.isLoading = true;
    this.exchangeApi.getExchangeData(this.selectedExchange).subscribe(data => {
      this.exchangeData = data;
      this.isLoading = false;
    });
  }
}
