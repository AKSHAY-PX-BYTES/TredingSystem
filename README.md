# 📈 AI Trading Strategy Predictor

A full-stack AI-based trading strategy prediction application built with **.NET 8**, **ASP.NET Core Web API**, and **Blazor WebAssembly**.

## 🏗️ Architecture

```
TradingSystem/
├── src/
│   ├── TradingSystem.Api/          # Backend Web API
│   │   ├── Controllers/            # REST API endpoints
│   │   ├── Services/               # Business logic (Clean Architecture)
│   │   ├── Models/                 # Domain models
│   │   ├── Middleware/             # Global exception handling
│   │   ├── Hubs/                   # SignalR real-time hub
│   │   ├── BackgroundServices/     # Market data broadcaster
│   │   └── Program.cs             # DI, CORS, Swagger setup
│   │
│   └── TradingSystem.Web/          # Blazor WebAssembly Frontend
│       ├── Pages/                  # Dashboard, News, Backtest pages
│       ├── Shared/                 # MainLayout
│       ├── Services/               # API client services
│       ├── Models/                 # Shared DTOs
│       └── wwwroot/                # Static assets, CSS, Chart.js
│
├── docker-compose.yml
├── Dockerfile.api
├── Dockerfile.web
└── TradingSystem.sln
```

## 🚀 Features

### Backend API
- **Market Data Service** — Simulated stock data with realistic price generation
- **Technical Indicators** — SMA (20/50/200), EMA (12/26), RSI, MACD, Bollinger Bands
- **News Sentiment Analysis** — Keyword-based sentiment scoring (Positive/Neutral/Negative)
- **AI Prediction Engine** — ML-inspired weighted signal combination
- **Strategy Engine** — BUY/SELL/HOLD signals with confidence scores and explanations
- **Backtesting** — Historical simulation with equity curves, trade history, Sharpe ratio
- **CSV Export** — Export market data and backtest results
- **SignalR** — Real-time quote updates
- **Swagger** — Interactive API documentation

### Frontend (Blazor WASM)
- **Dashboard** — Stock selector, price chart, strategy panel, indicators, news
- **News Analysis** — Custom headline sentiment analysis with charts
- **Backtesting** — Configure and run strategy simulations with visual results
- **Chart.js Integration** — Line charts, bar charts, doughnut charts
- **Modern Dark Theme** — Clean, responsive UI

## 📡 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/market/{symbol}` | Get stock quote |
| GET | `/market/{symbol}/indicators` | Get technical indicators |
| GET | `/market/{symbol}/history?days=30` | Get historical data |
| GET | `/market/{symbol}/export` | Export to CSV |
| POST | `/news/analyze` | Analyze news sentiment |
| GET | `/news/{symbol}` | Get latest news |
| GET | `/news/{symbol}/sentiment` | Get overall sentiment |
| GET | `/predict/{symbol}` | Get AI prediction |
| GET | `/strategy/{symbol}` | Get strategy evaluation |
| POST | `/backtest` | Run backtest simulation |
| POST | `/backtest/export` | Export backtest to CSV |

## 🛠️ Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- (Optional) [Docker](https://www.docker.com/)

### Run Locally

1. **Clone and restore:**
   ```bash
   cd TradingSystem
   dotnet restore
   ```

2. **Start the API (Terminal 1):**
   ```bash
   cd src/TradingSystem.Api
   dotnet run
   ```
   API runs at: `https://localhost:5001` | Swagger: `https://localhost:5001/swagger`

3. **Start the Frontend (Terminal 2):**
   ```bash
   cd src/TradingSystem.Web
   dotnet run
   ```
   Frontend runs at: `https://localhost:5002`

### Run with Docker

```bash
docker-compose up --build
```
- API: `http://localhost:5000`
- Web: `http://localhost:8080`

## 📊 Sample API Calls

```bash
# Get stock quote
curl https://localhost:5001/market/AAPL

# Get indicators
curl https://localhost:5001/market/AAPL/indicators

# Get strategy recommendation
curl https://localhost:5001/strategy/AAPL

# Get AI prediction
curl https://localhost:5001/predict/AAPL

# Analyze news sentiment
curl -X POST https://localhost:5001/news/analyze \
  -H "Content-Type: application/json" \
  -d '{"headlines":["Apple stock surges on record revenue","Tech sector faces recession concerns"]}'

# Run backtest
curl -X POST https://localhost:5001/backtest \
  -H "Content-Type: application/json" \
  -d '{"symbol":"AAPL","days":60,"initialCapital":10000,"strategy":"Combined"}'
```

## 🧠 How It Works

### Strategy Engine Flow
```
Market Data → Technical Indicators ─┐
                                    ├→ AI Prediction → Strategy Signal
News Headlines → Sentiment Score ───┘    (weighted)     (BUY/SELL/HOLD)
```

### Indicator Weights
| Signal | Weight |
|--------|--------|
| RSI | 20% |
| SMA Crossover | 20% |
| MACD | 15% |
| Bollinger Bands | 15% |
| Sentiment | 15% |
| Trend | 15% |

## 🔧 Tech Stack

- **Backend:** ASP.NET Core 8, ML.NET
- **Frontend:** Blazor WebAssembly
- **Charts:** Chart.js 4
- **Real-time:** SignalR
- **Docs:** Swagger/OpenAPI
- **Export:** CsvHelper
- **Container:** Docker + Nginx

## ⚠️ Disclaimer

This is a **demo/educational** application. The predictions and signals are based on simulated data and simplified models. **Do not use for actual trading decisions.**
