// API Response wrapper
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  timestamp: string;
}

// Market Data Models
export interface StockData {
  symbol: string;
  date: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
  change: number;
  changePercent: number;
}

export interface StockQuote {
  symbol: string;
  companyName: string;
  currentPrice: number;
  previousClose: number;
  dayHigh: number;
  dayLow: number;
  week52High: number;
  week52Low: number;
  marketCap: number;
  volume: number;
  change: number;
  changePercent: number;
  lastUpdated: string;
  priceCurrency: string;
  historicalData: StockData[];
}

export interface TechnicalIndicators {
  symbol: string;
  calculatedAt: string;
  currentPrice: number;
  sma20: number;
  sma50: number;
  sma200: number;
  ema12: number;
  ema26: number;
  rsi: number;
  macd: number;
  bollingerUpper: number;
  bollingerMiddle: number;
  bollingerLower: number;
  trend: string;
}

export interface NewsItem {
  headline: string;
  source: string;
  publishedAt: string;
  symbol?: string;
}

export interface NewsAnalysisRequest {
  headlines: string[];
  symbol?: string;
}

export interface SentimentResult {
  headline: string;
  sentiment: string;
  score: number;
  confidence: number;
}

export interface NewsAnalysisResponse {
  results: SentimentResult[];
  overallSentiment: string;
  averageScore: number;
  analyzedAt: string;
}

export interface PredictionResult {
  symbol: string;
  currentPrice: number;
  predictedPrice: number;
  predictedChange: number;
  predictedChangePercent: number;
  direction: string;
  confidence: number;
  timeHorizon: string;
  predictedAt: string;
  featureImportance: { [key: string]: number };
}

export interface StrategyResult {
  symbol: string;
  signal: string;
  confidence: number;
  explanation: string;
  currentPrice: number;
  targetPrice?: number;
  stopLoss?: number;
  indicators?: TechnicalIndicators;
  prediction?: PredictionResult;
  sentimentSummary?: string;
  generatedAt: string;
  reasons: string[];
}

export interface BacktestRequest {
  symbol: string;
  days: number;
  initialCapital: number;
  strategy: string;
}

export interface BacktestTrade {
  tradeNumber: number;
  entryDate: string;
  exitDate?: string;
  signal: string;
  entryPrice: number;
  exitPrice?: number;
  profitLoss?: number;
  profitLossPercent?: number;
  portfolioValue: number;
}

export interface BacktestResult {
  symbol: string;
  totalDays: number;
  initialCapital: number;
  finalCapital: number;
  totalReturn: number;
  totalReturnPercent: number;
  totalTrades: number;
  winningTrades: number;
  losingTrades: number;
  winRate: number;
  maxDrawdown: number;
  sharpeRatio: number;
  trades: BacktestTrade[];
  equityCurve: EquityPoint[];
  startDate: string;
  endDate: string;
  strategy: string;
}

export interface EquityPoint {
  date: string;
  value: number;
}

// Auth Models
export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  token?: string;
  error?: string;
  user?: UserInfo;
  expiresAt?: string;
}

export interface UserInfo {
  username: string;
  displayName: string;
  role: string;
  email?: string;
  plan: string;
  trialEndsAt?: string;
  isTrialExpired: boolean;
  hasAccess: boolean;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
  plan: string;
  phoneNumber?: string;
  countryCode?: string;
}

export interface RegisterResponse {
  success: boolean;
  message?: string;
  error?: string;
}

export interface SendOtpResponse {
  success: boolean;
  message?: string;
  error?: string;
  expiresAt?: string;
}

export interface VerifyOtpResponse {
  success: boolean;
  message?: string;
  error?: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ChangePasswordResponse {
  success: boolean;
  message?: string;
  error?: string;
}

// Currency Models
export interface CurrencyInfo {
  code: string;
  symbol: string;
  name: string;
  rateFromUsd: number;
}

export interface CurrencyListResponse {
  currencies: CurrencyInfo[];
  lastUpdated: string;
}

// Market Exchange Models
export interface MarketIndex {
  name: string;
  symbol: string;
  exchange: string;
  currentValue: number;
  previousClose: number;
  change: number;
  changePercent: number;
  lastUpdated: string;
  status: string;
}

export interface ExchangeStock {
  symbol: string;
  companyName: string;
  exchange: string;
  sector: string;
  currentPrice: number;
  previousClose: number;
  change: number;
  changePercent: number;
  dayHigh: number;
  dayLow: number;
  volume: number;
  marketCap: number;
  priceHistory: number[];
}

export interface ExchangeData {
  exchangeName: string;
  exchangeCode: string;
  country: string;
  flag: string;
  currency: string;
  status: string;
  timezone: string;
  indices: MarketIndex[];
  topGainers: ExchangeStock[];
  topLosers: ExchangeStock[];
  mostActive: ExchangeStock[];
  allStocks: ExchangeStock[];
  isLive: boolean;
  equityFutures: FnoContract[];
  equityOptions: FnoContract[];
  commodityFutures: FnoContract[];
  commodityOptions: FnoContract[];
}

export interface FnoContract {
  symbol: string;
  underlyingName: string;
  segment: string;
  instrumentType: string;
  expiry: string;
  strikePrice: number;
  lastPrice: number;
  change: number;
  changePercent: number;
  volume: number;
  openInterest: number;
  oiChange: number;
  lotSize: number;
  signal: string;
  impliedVolatility: number;
}

// Subscription Models
export interface SubscriptionInfo {
  plan: string;
  pricePerMonth: number;
  trialEndsAt?: string;
  isTrialActive: boolean;
  isTrialExpired: boolean;
  hasAccess: boolean;
  daysRemaining: number;
  features: string[];
}

export interface PlanTier {
  name: string;
  monthlyPrice: number;
  annualPrice: number;
  features: string[];
  isPopular: boolean;
}

export interface UpgradeRequest {
  plan: string;
  paymentMethod?: string;
}

export interface UpgradeResponse {
  success: boolean;
  message?: string;
  error?: string;
  subscription?: SubscriptionInfo;
}

// Notification Models
export interface NotificationDto {
  id: number;
  type: string;
  title: string;
  message: string;
  symbol?: string;
  isRead: boolean;
  createdAt: string;
}

export interface PriceAlertDto {
  id: number;
  symbol: string;
  targetPrice: number;
  thresholdPercent: number;
  direction: string;
  isTriggered: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface CreatePriceAlertRequest {
  symbol: string;
  targetPrice: number;
  thresholdPercent: number;
  direction: string;
}

// AI Models
export interface AiSignalDto {
  id: number;
  symbol: string;
  signalType: string;
  confidence: number;
  source: string;
  analysis: string;
  generatedAt: string;
}

export interface ChatRequest {
  message: string;
  sessionId?: string;
}

export interface ChatResponse {
  response: string;
  sessionId: string;
  suggestions?: string[];
  relatedSignals?: AiSignalDto[];
}

export interface LocaleInfo {
  code: string;
  name: string;
  nativeName: string;
  direction: string;
}

// Watchlist Models
export interface WatchlistItem {
  symbol: string;
  companyName: string;
  currentPrice: number;
  previousPrice: number;
  addedAt: string;
  lastUpdated: string;
}

// Feature Flag Model
export interface FeatureFlagItem {
  featureKey: string;
  displayName: string;
  description: string;
  isEnabled: boolean;
  updatedAt: string;
  updatedBy: string;
}

// Activity Models
export interface ActivityStatsModel {
  totalLogins: number;
  uniqueUsers: number;
  totalEvents: number;
  topCountries: { country: string; count: number }[];
}

export interface ActivityTimelineModel {
  date: string;
  logins: number;
  events: number;
}

export interface CountryStatsModel {
  countryCode: string;
  country: string;
  count: number;
}

export interface DeviceStatsModel {
  device: string;
  count: number;
}

export interface ActivityLogModel {
  id: number;
  username: string;
  eventType: string;
  ipAddress: string;
  countryCode: string;
  userAgent: string;
  createdAt: string;
  details: string;
}
