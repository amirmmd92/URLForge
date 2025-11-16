using Distributed_URL_Shortener_with_Analytics.Services;
using Distributed_URL_Shortener_with_Analytics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Distributed_URL_Shortener_with_Analytics.Controllers
{
    // Main controller for the web interface
    public class HomeController : Controller
    {
        private readonly IUrlShortenerService _urlShortenerService;
        private readonly IAnalyticsService _analyticsService;
        private readonly IApiKeyService _apiKeyService;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        public HomeController(
            IUrlShortenerService urlShortenerService,
            IAnalyticsService analyticsService,
            IApiKeyService apiKeyService,
            IUserService userService,
            IConfiguration configuration)
        {
            _urlShortenerService = urlShortenerService;
            _analyticsService = analyticsService;
            _apiKeyService = apiKeyService;
            _userService = userService;
            _configuration = configuration;
        }

        // Home page - show URL shortener form
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Set admin status if user is logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var userId))
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    ViewBag.IsAdmin = user?.IsAdmin ?? false;
                }
            }
            return View();
        }

        // Create short link (POST)
        [HttpPost]
        public async Task<IActionResult> CreateShortLink(string originalUrl, string? customCode = null)
        {
            if (string.IsNullOrWhiteSpace(originalUrl))
            {
                return Json(new { success = false, message = "URL is required" });
            }

            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var id))
                {
                    userId = id;
                }
            }

            var shortLink = await _urlShortenerService.CreateShortLinkAsync(originalUrl, userId, customCode);

            if (shortLink == null)
            {
                return Json(new { success = false, message = "Failed to create short link. URL may be invalid or custom code already exists." });
            }

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host.Value}";
            var shortUrl = $"{baseUrl}/r/{shortLink.ShortCode}";

            return Json(new
            {
                success = true,
                shortUrl = shortUrl,
                shortCode = shortLink.ShortCode,
                hasAnalytics = shortLink.HasAnalytics
            });
        }

        // Dashboard - show user's links (requires login)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return RedirectToAction("Index");
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Index");
            }

            var links = await _urlShortenerService.GetUserLinksAsync(user.Id);
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host.Value}";

            ViewBag.BaseUrl = baseUrl;
            ViewBag.IsAdmin = user.IsAdmin;
            return View(links);
        }

        // Analytics page for a specific link (requires login and ownership)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Analytics(int id)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return RedirectToAction("Index");
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Index");
            }

            var analytics = await _analyticsService.GetLinkAnalyticsAsync(id, user.Id);

            if (analytics == null)
            {
                return NotFound();
            }

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host.Value}";
            ViewBag.BaseUrl = baseUrl;
            ViewBag.IsAdmin = user.IsAdmin;
            return View(analytics);
        }

        // API Keys management page (requires login)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ApiKeys()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return RedirectToAction("Index");
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Index");
            }

            var apiKeys = await _apiKeyService.GetUserApiKeysAsync(user.Id);
            ViewBag.IsAdmin = user.IsAdmin;
            return View(apiKeys);
        }

        // Create API key (POST)
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateApiKey(string? description = null)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Json(new { success = false, message = "User not found" });
            }

            var apiKey = await _apiKeyService.CreateApiKeyAsync(userId, description);

            if (apiKey == null)
            {
                return Json(new { success = false, message = "Failed to create API key" });
            }

            return Json(new { success = true, apiKey = apiKey.Key, id = apiKey.Id });
        }

        // Revoke API key (POST)
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RevokeApiKey(int id)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Json(new { success = false, message = "User not found" });
            }

            var result = await _apiKeyService.RevokeApiKeyAsync(id, userId);
            return Json(new { success = result });
        }

        // Delete link (POST)
        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteLink(int id)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Json(new { success = false, message = "User not found" });
            }

            var result = await _urlShortenerService.DeleteLinkAsync(id, userId);
            return Json(new { success = result });
        }

        // Error page
        [HttpGet]
        public IActionResult Error()
        {
            return View();
        }
    }
}

