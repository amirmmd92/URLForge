using Distributed_URL_Shortener_with_Analytics.Services;
using Distributed_URL_Shortener_with_Analytics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Distributed_URL_Shortener_with_Analytics.Controllers
{
    // API controller for external access
    [ApiController]
    [Route("api/[controller]")]
    public class ApiController : ControllerBase
    {
        private readonly IUrlShortenerService _urlShortenerService;
        private readonly IAnalyticsService _analyticsService;
        private readonly IApiKeyService _apiKeyService;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiController> _logger;

        public ApiController(
            IUrlShortenerService urlShortenerService,
            IAnalyticsService analyticsService,
            IApiKeyService apiKeyService,
            IUserService userService,
            IConfiguration configuration,
            ILogger<ApiController> logger)
        {
            _urlShortenerService = urlShortenerService;
            _analyticsService = analyticsService;
            _apiKeyService = apiKeyService;
            _userService = userService;
            _configuration = configuration;
            _logger = logger;
        }

        // Middleware to validate API key
        private async Task<User?> GetUserFromApiKeyAsync()
        {
            if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
            {
                return null;
            }

            var apiKey = apiKeyHeader.ToString();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return null;
            }

            var user = await _apiKeyService.GetUserByApiKeyAsync(apiKey);
            if (user != null)
            {
                await _apiKeyService.UpdateLastUsedAsync(apiKey);
            }

            return user;
        }

        // Create short link via API
        [HttpPost("shorten")]
        public async Task<IActionResult> Shorten([FromBody] ShortenRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Url))
            {
                return BadRequest(new { error = "URL is required" });
            }

            // Get user from API key (optional - allows anonymous links)
            User? user = null;
            if (Request.Headers.ContainsKey("X-API-Key"))
            {
                user = await GetUserFromApiKeyAsync();
                if (user == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }
            }

            var shortLink = await _urlShortenerService.CreateShortLinkAsync(
                model.Url,
                user?.Id,
                model.CustomCode);

            if (shortLink == null)
            {
                return BadRequest(new { error = "Failed to create short link. URL may be invalid or custom code already exists." });
            }

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host.Value}";
            var shortUrl = $"{baseUrl}/r/{shortLink.ShortCode}";

            return Ok(new
            {
                success = true,
                shortUrl = shortUrl,
                shortCode = shortLink.ShortCode,
                originalUrl = shortLink.OriginalUrl,
                hasAnalytics = shortLink.HasAnalytics
            });
        }

        // Get analytics for a link (requires API key and ownership)
        [HttpGet("analytics/{linkId}")]
        public async Task<IActionResult> GetAnalytics(int linkId)
        {
            var user = await GetUserFromApiKeyAsync();
            if (user == null)
            {
                return Unauthorized(new { error = "Valid API key required" });
            }

            var analytics = await _analyticsService.GetLinkAnalyticsAsync(linkId, user.Id);
            if (analytics == null)
            {
                return NotFound(new { error = "Link not found or you don't have access" });
            }

            return Ok(analytics);
        }

        // Get all user links (requires API key)
        [HttpGet("links")]
        public async Task<IActionResult> GetLinks()
        {
            var user = await GetUserFromApiKeyAsync();
            if (user == null)
            {
                return Unauthorized(new { error = "Valid API key required" });
            }

            var links = await _urlShortenerService.GetUserLinksAsync(user.Id);
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host.Value}";

            var result = links.Select(link => new
            {
                id = link.Id,
                shortCode = link.ShortCode,
                shortUrl = $"{baseUrl}/r/{link.ShortCode}",
                originalUrl = link.OriginalUrl,
                createdAt = link.CreatedAt,
                hasAnalytics = link.HasAnalytics,
                clickCount = link.ClickAnalytics?.Count ?? 0
            });

            return Ok(result);
        }

        // Delete a link (requires API key and ownership)
        [HttpDelete("links/{linkId}")]
        public async Task<IActionResult> DeleteLink(int linkId)
        {
            var user = await GetUserFromApiKeyAsync();
            if (user == null)
            {
                return Unauthorized(new { error = "Valid API key required" });
            }

            var result = await _urlShortenerService.DeleteLinkAsync(linkId, user.Id);
            if (!result)
            {
                return NotFound(new { error = "Link not found or you don't have access" });
            }

            return Ok(new { success = true });
        }
    }

    // Request model for API
    public class ShortenRequest
    {
        public string Url { get; set; } = string.Empty;
        public string? CustomCode { get; set; }
    }
}

