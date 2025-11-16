using Distributed_URL_Shortener_with_Analytics.Data;
using Distributed_URL_Shortener_with_Analytics.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Distributed_URL_Shortener_with_Analytics.Services
{
    // Service for API key management
    public interface IApiKeyService
    {
        Task<ApiKey?> CreateApiKeyAsync(int userId, string? description = null);
        Task<bool> ValidateApiKeyAsync(string apiKey);
        Task<User?> GetUserByApiKeyAsync(string apiKey);
        Task<List<ApiKey>> GetUserApiKeysAsync(int userId);
        Task<bool> RevokeApiKeyAsync(int apiKeyId, int userId);
        Task UpdateLastUsedAsync(string apiKey);
    }

    public class ApiKeyService : IApiKeyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApiKeyService> _logger;

        public ApiKeyService(ApplicationDbContext context, ILogger<ApiKeyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Generate a secure API key
        private string GenerateApiKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var key = new string(Enumerable.Repeat(chars, 32)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            
            // Add prefix for identification
            return $"duswa_" + key;
        }

        // Create a new API key for user
        public async Task<ApiKey?> CreateApiKeyAsync(int userId, string? description = null)
        {
            try
            {
                string apiKey;
                bool keyExists;
                
                // Ensure unique key
                do
                {
                    apiKey = GenerateApiKey();
                    keyExists = await _context.ApiKeys.AnyAsync(a => a.Key == apiKey);
                } while (keyExists);

                var newApiKey = new ApiKey
                {
                    UserId = userId,
                    Key = apiKey,
                    Description = description,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ApiKeys.Add(newApiKey);
                await _context.SaveChangesAsync();

                return newApiKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API key");
                return null;
            }
        }

        // Validate API key
        public async Task<bool> ValidateApiKeyAsync(string apiKey)
        {
            try
            {
                var key = await _context.ApiKeys
                    .FirstOrDefaultAsync(a => a.Key == apiKey && a.IsActive);

                if (key == null)
                {
                    return false;
                }

                // Check expiration
                if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key");
                return false;
            }
        }

        // Get user by API key
        public async Task<User?> GetUserByApiKeyAsync(string apiKey)
        {
            try
            {
                var key = await _context.ApiKeys
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Key == apiKey && a.IsActive);

                if (key == null)
                {
                    return null;
                }

                // Check expiration
                if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return null;
                }

                return key.User;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by API key");
                return null;
            }
        }

        // Get all API keys for a user
        public async Task<List<ApiKey>> GetUserApiKeysAsync(int userId)
        {
            return await _context.ApiKeys
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        // Revoke API key
        public async Task<bool> RevokeApiKeyAsync(int apiKeyId, int userId)
        {
            try
            {
                var apiKey = await _context.ApiKeys
                    .FirstOrDefaultAsync(a => a.Id == apiKeyId && a.UserId == userId);

                if (apiKey == null)
                {
                    return false;
                }

                apiKey.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key");
                return false;
            }
        }

        // Update last used timestamp
        public async Task UpdateLastUsedAsync(string apiKey)
        {
            try
            {
                var key = await _context.ApiKeys
                    .FirstOrDefaultAsync(a => a.Key == apiKey && a.IsActive);

                if (key != null)
                {
                    key.LastUsedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last used");
            }
        }
    }
}

