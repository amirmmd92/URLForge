using Distributed_URL_Shortener_with_Analytics.Data;
using Distributed_URL_Shortener_with_Analytics.Models;
using Microsoft.EntityFrameworkCore;

namespace Distributed_URL_Shortener_with_Analytics.Services
{
    // Service for analytics operations
    public interface IAnalyticsService
    {
        Task<AnalyticsViewModel?> GetLinkAnalyticsAsync(int linkId, int userId);
        Task<List<AnalyticsViewModel>> GetAllUserAnalyticsAsync(int userId);
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Get analytics for a specific link (only if user owns it)
        public async Task<AnalyticsViewModel?> GetLinkAnalyticsAsync(int linkId, int userId)
        {
            try
            {
                var link = await _context.ShortLinks
                    .Include(s => s.ClickAnalytics)
                    .FirstOrDefaultAsync(s => s.Id == linkId && s.UserId == userId);

                if (link == null || !link.HasAnalytics)
                {
                    return null;
                }

                var analytics = link.ClickAnalytics.ToList();

                return new AnalyticsViewModel
                {
                    LinkId = link.Id,
                    ShortCode = link.ShortCode,
                    OriginalUrl = link.OriginalUrl,
                    TotalClicks = analytics.Count,
                    UniqueClicks = analytics.Select(a => a.IpAddress).Distinct().Count(),
                    ClicksByDate = analytics
                        .GroupBy(a => a.ClickedAt.Date)
                        .Select(g => new ClicksByDate { Date = g.Key, Count = g.Count() })
                        .OrderBy(c => c.Date)
                        .ToList(),
                    ClicksByCountry = analytics
                        .Where(a => !string.IsNullOrEmpty(a.Country))
                        .GroupBy(a => a.Country)
                        .Select(g => new ClicksByCountry { Country = g.Key!, Count = g.Count() })
                        .OrderByDescending(c => c.Count)
                        .ToList(),
                    RecentClicks = analytics
                        .OrderByDescending(a => a.ClickedAt)
                        .Take(50)
                        .Select(a => new RecentClick
                        {
                            IpAddress = a.IpAddress,
                            UserAgent = a.UserAgent,
                            Referer = a.Referer,
                            ClickedAt = a.ClickedAt
                        })
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting link analytics");
                return null;
            }
        }

        // Get analytics for all user links
        public async Task<List<AnalyticsViewModel>> GetAllUserAnalyticsAsync(int userId)
        {
            try
            {
                var links = await _context.ShortLinks
                    .Include(s => s.ClickAnalytics)
                    .Where(s => s.UserId == userId && s.HasAnalytics)
                    .ToListAsync();

                return links.Select(link => new AnalyticsViewModel
                {
                    LinkId = link.Id,
                    ShortCode = link.ShortCode,
                    OriginalUrl = link.OriginalUrl,
                    TotalClicks = link.ClickAnalytics.Count,
                    UniqueClicks = link.ClickAnalytics.Select(a => a.IpAddress).Distinct().Count(),
                    ClicksByDate = link.ClickAnalytics
                        .GroupBy(a => a.ClickedAt.Date)
                        .Select(g => new ClicksByDate { Date = g.Key, Count = g.Count() })
                        .OrderBy(c => c.Date)
                        .ToList(),
                    ClicksByCountry = link.ClickAnalytics
                        .Where(a => !string.IsNullOrEmpty(a.Country))
                        .GroupBy(a => a.Country)
                        .Select(g => new ClicksByCountry { Country = g.Key!, Count = g.Count() })
                        .OrderByDescending(c => c.Count)
                        .ToList(),
                    RecentClicks = link.ClickAnalytics
                        .OrderByDescending(a => a.ClickedAt)
                        .Take(10)
                        .Select(a => new RecentClick
                        {
                            IpAddress = a.IpAddress,
                            UserAgent = a.UserAgent,
                            Referer = a.Referer,
                            ClickedAt = a.ClickedAt
                        })
                        .ToList()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all user analytics");
                return new List<AnalyticsViewModel>();
            }
        }
    }

    // View models for analytics
    public class AnalyticsViewModel
    {
        public int LinkId { get; set; }
        public string ShortCode { get; set; } = string.Empty;
        public string OriginalUrl { get; set; } = string.Empty;
        public int TotalClicks { get; set; }
        public int UniqueClicks { get; set; }
        public List<ClicksByDate> ClicksByDate { get; set; } = new();
        public List<ClicksByCountry> ClicksByCountry { get; set; } = new();
        public List<RecentClick> RecentClicks { get; set; } = new();
    }

    public class ClicksByDate
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class ClicksByCountry
    {
        public string Country { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RecentClick
    {
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Referer { get; set; }
        public DateTime ClickedAt { get; set; }
    }
}

