using Distributed_URL_Shortener_with_Analytics.Services;
using Microsoft.AspNetCore.Mvc;

namespace Distributed_URL_Shortener_with_Analytics.Controllers
{
    // Controller to handle short link redirects
    [Route("r")]
    public class RedirectController : Controller
    {
        private readonly IUrlShortenerService _urlShortenerService;
        private readonly ILogger<RedirectController> _logger;

        public RedirectController(IUrlShortenerService urlShortenerService, ILogger<RedirectController> logger)
        {
            _urlShortenerService = urlShortenerService;
            _logger = logger;
        }

        // GET /r/{shortCode}
        [HttpGet("{shortCode}")]
        public async Task<IActionResult> RedirectToOriginal(string shortCode)
        {
            if (string.IsNullOrWhiteSpace(shortCode))
                return NotFound();

            var shortLink = await _urlShortenerService.GetShortLinkByCodeAsync(shortCode);
            if (shortLink == null)
                return NotFound();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();
            var referer = Request.Headers["Referer"].ToString();

            await _urlShortenerService.RedirectAsync(shortCode, ipAddress, userAgent, referer);

            // ریدایرکت واقعی
            return Redirect(shortLink.OriginalUrl);
        }
    }

}

