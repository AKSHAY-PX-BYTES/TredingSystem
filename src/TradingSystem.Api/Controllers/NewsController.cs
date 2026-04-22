using Microsoft.AspNetCore.Mvc;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class NewsController : ControllerBase
{
    private readonly INewsService _newsService;
    private readonly ILogger<NewsController> _logger;

    public NewsController(INewsService newsService, ILogger<NewsController> logger)
    {
        _newsService = newsService;
        _logger = logger;
    }

    /// <summary>
    /// Analyze news headlines for sentiment
    /// </summary>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(ApiResponse<NewsAnalysisResponse>), 200)]
    public async Task<IActionResult> Analyze([FromBody] NewsAnalysisRequest request)
    {
        _logger.LogInformation("POST /news/analyze with {Count} headlines", request.Headlines.Count);

        if (request.Headlines == null || !request.Headlines.Any())
            return BadRequest(ApiResponse<NewsAnalysisResponse>.Fail("At least one headline is required"));

        var result = await _newsService.AnalyzeAsync(request);
        return Ok(ApiResponse<NewsAnalysisResponse>.Ok(result));
    }

    /// <summary>
    /// Get latest news for a symbol
    /// </summary>
    [HttpGet("{symbol}")]
    [ProducesResponseType(typeof(ApiResponse<List<NewsItem>>), 200)]
    public async Task<IActionResult> GetNews(string symbol)
    {
        _logger.LogInformation("GET /news/{Symbol}", symbol);
        var news = await _newsService.GetLatestNewsAsync(symbol);
        return Ok(ApiResponse<List<NewsItem>>.Ok(news));
    }

    /// <summary>
    /// Get overall sentiment for a symbol based on latest news
    /// </summary>
    [HttpGet("{symbol}/sentiment")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> GetSentiment(string symbol)
    {
        _logger.LogInformation("GET /news/{Symbol}/sentiment", symbol);
        var sentiment = await _newsService.GetOverallSentimentAsync(symbol);
        return Ok(ApiResponse<object>.Ok(new { Symbol = symbol.ToUpperInvariant(), Sentiment = sentiment }));
    }
}
