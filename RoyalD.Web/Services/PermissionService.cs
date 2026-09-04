using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RoyalD.Web.Models;
using System.Security.Claims;

namespace RoyalD.Web.Services
{
    public class PermissionService
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PermissionService(AppDbContext db, IMemoryCache cache, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> CanAccessAsync(string menuKey)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity.IsAuthenticated) return false;

            if (user.IsInRole("admin") || (user.Identity.Name?.ToLower() == "admin"))
                return true;

            string username = user.Identity.Name ?? "";
            
            string cacheKey = "UserPerm_{username}_{menuKey}";
            if (_cache.TryGetValue(cacheKey, out bool isAllowed))
            {
                return isAllowed;
            }

            var perm = await _db.UserMenuPermissions.FirstOrDefaultAsync(p => p.Username == username && p.MenuKey == menuKey);
            
            // Default to true for now if not set, or false? Let's default to false unless they are admin,
            // BUT wait! If it's a new feature, maybe default to true for existing users so they don't lose access suddenly?
            // Actually, we'll implement default logic based on menuKey.
            bool allowed = true;
            if (perm != null)
            {
                allowed = perm.IsAllowed;
            }
            else
            {
                var positionClaim = user.FindFirst("Position")?.Value ?? "";
                bool isSalesRep = positionClaim == "ผู้แทนขาย" || positionClaim == "พนักงานขาย" || positionClaim.Contains("ผู้แทน") || positionClaim.Contains("พนักงานขาย");

                if (menuKey == "Settings" || menuKey == "Upload") allowed = false;
                if (menuKey == "AdminPermissions") allowed = false;
            }

            _cache.Set(cacheKey, allowed, TimeSpan.FromMinutes(15));
            return allowed;
        }
    }
}
