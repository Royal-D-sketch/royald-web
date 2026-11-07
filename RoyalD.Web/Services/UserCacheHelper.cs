using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RoyalD.Web.Models;

namespace RoyalD.Web.Services
{
    public static class UserCacheHelper
    {
        public static async Task<AppUser?> GetCurrentUserAsync(AppDbContext db, IMemoryCache? cache, string? username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            string normalized = username.Trim();
            if (cache != null)
            {
                return await cache.GetOrCreateAsync($"user_obj_{normalized.ToLower()}", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                    return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username.ToLower() == normalized.ToLower());
                });
            }

            return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username.ToLower() == normalized.ToLower());
        }

        public static void InvalidateUser(IMemoryCache? cache, string? username)
        {
            if (cache != null && !string.IsNullOrWhiteSpace(username))
            {
                cache.Remove($"user_obj_{username.Trim().ToLower()}");
                cache.Remove($"user_perm_{username.Trim()}");
            }
        }
    }
}
