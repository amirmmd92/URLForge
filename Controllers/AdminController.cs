using Distributed_URL_Shortener_with_Analytics.Data;
using Distributed_URL_Shortener_with_Analytics.Models;
using Distributed_URL_Shortener_with_Analytics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Distributed_URL_Shortener_with_Analytics.Controllers
{
    // Admin controller - requires admin role
    [Authorize]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            IUserService userService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        // Check if current user is admin
        private async Task<bool> IsAdminAsync()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return false;
            }

            var user = await _userService.GetUserByIdAsync(userId);
            return user?.IsAdmin == true;
        }

        // Admin dashboard
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var stats = new AdminStatsViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalLinks = await _context.ShortLinks.CountAsync(),
                TotalClicks = await _context.ClickAnalytics.CountAsync(),
                ActiveLinks = await _context.ShortLinks.CountAsync(s => s.IsActive),
                LinksWithAnalytics = await _context.ShortLinks.CountAsync(s => s.UserId.HasValue)
            };

            ViewBag.IsAdmin = true;
            return View(stats);
        }

        // Users management page
        [HttpGet("users")]
        public async Task<IActionResult> Users()
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var users = await _context.Users
                .Include(u => u.ShortLinks)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            ViewBag.IsAdmin = true;
            return View(users);
        }

        // Links management page
        [HttpGet("links")]
        public async Task<IActionResult> Links()
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var links = await _context.ShortLinks
                .Include(s => s.User)
                .Include(s => s.ClickAnalytics)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.IsAdmin = true;
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host.Value}";
            ViewBag.BaseUrl = baseUrl;
            return View(links);
        }

        // Toggle admin status
        [HttpPost("users/{userId}/toggle-admin")]
        public async Task<IActionResult> ToggleAdmin(int userId)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent removing admin status from yourself
            var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdClaim, out var currentUserId) && currentUserId == userId)
            {
                return BadRequest(new { error = "Cannot remove admin status from yourself" });
            }

            var result = await _userService.SetAdminStatusAsync(userId, !user.IsAdmin);
            return Json(new { success = result, isAdmin = !user.IsAdmin });
        }

        // Delete user (admin only)
        [HttpPost("users/{userId}/delete")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent deleting yourself
            var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdClaim, out var currentUserId) && currentUserId == userId)
            {
                return BadRequest(new { error = "Cannot delete yourself" });
            }

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return Json(new { success = false, error = "Failed to delete user" });
            }
        }

        // Delete link (admin only)
        [HttpPost("links/{linkId}/delete")]
        public async Task<IActionResult> DeleteLink(int linkId)
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var link = await _context.ShortLinks.FindAsync(linkId);
            if (link == null)
            {
                return NotFound();
            }

            try
            {
                link.IsActive = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting link");
                return Json(new { success = false, error = "Failed to delete link" });
            }
        }
    }

    // View model for admin stats
    public class AdminStatsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalLinks { get; set; }
        public int TotalClicks { get; set; }
        public int ActiveLinks { get; set; }
        public int LinksWithAnalytics { get; set; }
    }
}

