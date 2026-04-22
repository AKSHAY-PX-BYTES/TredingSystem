using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface IExportService
{
    Task<byte[]> ExportMarketDataToCsvAsync(string symbol, int days);
    Task<byte[]> ExportBacktestToCsvAsync(BacktestResult result);
}

public class ExportService : IExportService
{
    private readonly IMarketDataService _marketDataService;

    public ExportService(IMarketDataService marketDataService)
    {
        _marketDataService = marketDataService;
    }

    public async Task<byte[]> ExportMarketDataToCsvAsync(string symbol, int days)
    {
        var data = await _marketDataService.GetHistoricalDataAsync(symbol, days);

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        csv.WriteRecords(data.Select(d => new
        {
            d.Symbol,
            Date = d.Date.ToString("yyyy-MM-dd"),
            d.Open,
            d.High,
            d.Low,
            d.Close,
            d.Volume,
            d.Change,
            d.ChangePercent
        }));

        await writer.FlushAsync();
        return memoryStream.ToArray();
    }

    public Task<byte[]> ExportBacktestToCsvAsync(BacktestResult result)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        csv.WriteRecords(result.Trades.Select(t => new
        {
            t.TradeNumber,
            EntryDate = t.EntryDate.ToString("yyyy-MM-dd"),
            ExitDate = t.ExitDate?.ToString("yyyy-MM-dd"),
            t.Signal,
            t.EntryPrice,
            t.ExitPrice,
            t.ProfitLoss,
            t.ProfitLossPercent,
            t.PortfolioValue
        }));

        writer.Flush();
        return Task.FromResult(memoryStream.ToArray());
    }
}
