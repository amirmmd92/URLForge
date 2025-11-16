using Distributed_URL_Shortener_with_Analytics.Data;
using Distributed_URL_Shortener_with_Analytics.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Distributed_URL_Shortener_with_Analytics.Services
{
    // Service for user management
    public interface IUserService
    {
        Task<User?> RegisterAsync(string username, string email, string password);
        Task<User?> LoginAsync(string username, string password);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<bool> SetAdminStatusAsync(int userId, bool isAdmin);
        Task<List<User>> GetAllUsersAsync();
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Register new user
        public async Task<User?> RegisterAsync(string username, string email, string password)
        {
            try
            {
                // Check if username already exists
                if (await _context.Users.AnyAsync(u => u.Username == username))
                {
                    return null; // Username already exists
                }

                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == email))
                {
                    return null; // Email already exists
                }

                // Check if this is the first user (make them admin)
                var isFirstUser = !await _context.Users.AnyAsync();
                var firstAdminCreated = await _context.AppSettings
                    .AnyAsync(s => s.Key == "FirstAdminCreated" && s.Value == "true");

                // Hash password
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                var user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = passwordHash,
                    IsAdmin = isFirstUser && !firstAdminCreated, // First user becomes admin
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Mark that first admin has been created
                if (isFirstUser && !firstAdminCreated)
                {
                    var setting = new AppSettings
                    {
                        Key = "FirstAdminCreated",
                        Value = "true",
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.AppSettings.Add(setting);
                    await _context.SaveChangesAsync();
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
                return null;
            }
        }

        // Login user
        public async Task<User?> LoginAsync(string username, string password)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    return null; // User not found
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    return null; // Invalid password
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging in user");
                return null;
            }
        }

        // Get user by ID
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        // Get user by username
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        // Set admin status (only for admin operations)
        public async Task<bool> SetAdminStatusAsync(int userId, bool isAdmin)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.IsAdmin = isAdmin;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting admin status");
                return false;
            }
        }

        // Get all users (for admin panel)
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }
    }
}

