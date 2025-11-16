using Distributed_URL_Shortener_with_Analytics.Data;
using Distributed_URL_Shortener_with_Analytics.Models;
using Microsoft.EntityFrameworkCore;

namespace Distributed_URL_Shortener_with_Analytics.Services
{
    // Service for URL shortening operations
    public interface IUrlShortenerService
    {
        Task<ShortLink?> CreateShortLinkAsync(string originalUrl, int? userId = null, string? customCode = null);
        Task<ShortLink?> GetShortLinkByCodeAsync(string shortCode);
        Task<bool> RedirectAsync(string shortCode, string? ipAddress, string? userAgent, string? referer);
        Task<List<ShortLink>> GetUserLinksAsync(int userId);
        Task<bool> DeleteLinkAsync(int linkId, int userId);
    }

    public class UrlShortenerService : IUrlShortenerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UrlShortenerService> _logger;

        public UrlShortenerService(ApplicationDbContext context, ILogger<UrlShortenerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Generate a random short code
        private string GenerateShortCode(int length = 6)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Create a new short link
        public async Task<ShortLink?> CreateShortLinkAsync(string originalUrl, int? userId = null, string? customCode = null)
        {
            try
            {
                // Validate URL
                if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out var uri))
                {
                    return null;
                }

                // Check if custom code is provided and available
                string shortCode;
                if (!string.IsNullOrWhiteSpace(customCode))
                {
                    // Validate custom code (alphanumeric and dash/underscore only)
                    if (!System.Text.RegularExpressions.Regex.IsMatch(customCode, @"^[a-zA-Z0-9_-]+$"))
                    {
                        return null;
                    }

                    // Check if code already exists
                    var exists = await _context.ShortLinks.AnyAsync(s => s.ShortCode == customCode);
                    if (exists)
                    {
                        return null;
                    }
                    shortCode = customCode;
                }
                else
                {
                    // Generate unique code
                    string newCode;
                    bool codeExists;
                    do
                    {
                        newCode = GenerateShortCode();
                        codeExists = await _context.ShortLinks.AnyAsync(s => s.ShortCode == newCode);
                    } while (codeExists);

                    shortCode = newCode;
                }

                var shortLink = new ShortLink
                {
                    ShortCode = shortCode,
                    OriginalUrl = originalUrl,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ShortLinks.Add(shortLink);
                await _context.SaveChangesAsync();

                return shortLink;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating short link");
                return null;
            }
        }

        // Get short link by code
        public async Task<ShortLink?> GetShortLinkByCodeAsync(string shortCode)
        {
            return await _context.ShortLinks
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.ShortCode == shortCode && s.IsActive);
        }

        // Handle redirect and track analytics
        public async Task<bool> RedirectAsync(string shortCode, string? ipAddress, string? userAgent, string? referer)
        {
            try
            {
                var shortLink = await GetShortLinkByCodeAsync(shortCode);
                if (shortLink == null)
                {
                    return false;
                }

                // Check if link has expired
                if (shortLink.ExpiresAt.HasValue && shortLink.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return false;
                }

                // Only track analytics if link was created by a logged-in user
                if (shortLink.HasAnalytics)
                {
                    var analytics = new ClickAnalytics
                    {
                        ShortLinkId = shortLink.Id,
                        IpAddress = ipAddress,
                        UserAgent = userAgent,
                        Referer = referer,
                        ClickedAt = DateTime.UtcNow
                    };

                    _context.ClickAnalytics.Add(analytics);
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during redirect");
                return false;
            }
        }

        // Get all links for a user (only active links)
        public async Task<List<ShortLink>> GetUserLinksAsync(int userId)
        {
            return await _context.ShortLinks
                .Include(s => s.ClickAnalytics)
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        // Delete a link (soft delete by setting IsActive to false)
        public async Task<bool> DeleteLinkAsync(int linkId, int userId)
        {
            try
            {
                var link = await _context.ShortLinks
                    .FirstOrDefaultAsync(s => s.Id == linkId && s.UserId == userId);

                if (link == null)
                {
                    return false;
                }

                link.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting link");
                return false;
            }
        }
    }
}

