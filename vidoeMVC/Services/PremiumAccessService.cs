using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using vidoeMVC.DAL;
using vidoeMVC.Enums;
using vidoeMVC.Models;

namespace vidoeMVC.Services
{
    public interface IPremiumAccessService
    {
        Task<bool> HasPremiumAccessAsync(string userId);
        Task<bool> CanAccessVideoAsync(string userId, Video video);
        Task UpdateExpiredSubscriptionsAsync();
    }

    public class PremiumAccessService : IPremiumAccessService
    {
        private readonly VidoeDBContext _context;
        private readonly UserManager<AppUser> _userManager;

        public PremiumAccessService(VidoeDBContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<bool> HasPremiumAccessAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Check if user has premium and it hasn't expired
            return user.IsPremium && (user.PremiumExpiryDate == null || user.PremiumExpiryDate > DateTime.Now);
        }

        public async Task<bool> CanAccessVideoAsync(string userId, Video video)
        {
            // Public videos can be accessed by anyone
            if (video.Privacy?.Contains(VideoStatus.Public) == true)
                return true;

            // Private videos can only be accessed by the owner
            if (video.Privacy?.Contains(VideoStatus.Private) == true)
                return video.AuthorId == userId;

            // Premium videos require premium access
            if (video.Privacy?.Contains(VideoStatus.Premium) == true)
            {
                // Allow author to access their own premium videos
                if (video.AuthorId == userId)
                    return true;

                // Check if user has premium access
                return await HasPremiumAccessAsync(userId);
            }

            return false;
        }

        public async Task UpdateExpiredSubscriptionsAsync()
        {
            // Find all active subscriptions that have expired
            var expiredSubscriptions = await _context.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate < DateTime.Now)
                .ToListAsync();

            foreach (var subscription in expiredSubscriptions)
            {
                subscription.Status = SubscriptionStatus.Expired;
                subscription.UpdatedTime = DateTime.Now;

                // Update user premium status
                var user = await _userManager.FindByIdAsync(subscription.UserId);
                if (user != null)
                {
                    user.IsPremium = false;
                    user.PremiumExpiryDate = null;
                    await _userManager.UpdateAsync(user);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}