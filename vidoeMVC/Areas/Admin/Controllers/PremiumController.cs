using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using vidoeMVC.DAL;
using vidoeMVC.Enums;
using vidoeMVC.Models;
using vidoeMVC.Services;

namespace vidoeMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class PremiumController : Controller
    {
        private readonly VidoeDBContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IPremiumAccessService _premiumAccessService;

        public PremiumController(VidoeDBContext context, UserManager<AppUser> userManager, IPremiumAccessService premiumAccessService)
        {
            _context = context;
            _userManager = userManager;
            _premiumAccessService = premiumAccessService;
        }

        // GET: Admin/Premium
        public async Task<IActionResult> Index()
        {
            var stats = new PremiumStatsViewModel
            {
                TotalPremiumUsers = await _context.Users.CountAsync(u => u.IsPremium),
                TotalPremiumVideos = await _context.Videos.CountAsync(v => v.Privacy.Contains(VideoStatus.Premium)),
                TotalActiveSubscriptions = await _context.Subscriptions.CountAsync(s => s.Status == SubscriptionStatus.Active),
                TotalRevenue = await _context.Payments.Where(p => p.Status == PaymentStatus.Completed).SumAsync(p => p.Amount)
            };

            return View(stats);
        }

        // GET: Admin/Premium/Users
        public async Task<IActionResult> Users()
        {
            var premiumUsers = await _context.Users
                .Where(u => u.IsPremium)
                .Include(u => u.Subscriptions)
                .Select(u => new PremiumUserViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    Email = u.Email ?? "",
                    Name = u.Name,
                    IsPremium = u.IsPremium,
                    PremiumExpiryDate = u.PremiumExpiryDate,
                    ActiveSubscription = u.Subscriptions!.FirstOrDefault(s => s.Status == SubscriptionStatus.Active)
                })
                .ToListAsync();

            return View(premiumUsers);
        }

        // GET: Admin/Premium/Videos
        public async Task<IActionResult> Videos()
        {
            var premiumVideos = await _context.Videos
                .Where(v => v.Privacy.Contains(VideoStatus.Premium))
                .Include(v => v.Author)
                .Include(v => v.VCategories).ThenInclude(vc => vc.Category)
                .Select(v => new PremiumVideoViewModel
                {
                    Id = v.Id,
                    Title = v.Title,
                    AuthorName = v.Author!.UserName ?? "",
                    CreatedTime = v.CreatedTime,
                    ViewCount = v.ViewCount,
                    Categories = v.VCategories!.Select(vc => vc.Category!.Name).ToList()
                })
                .ToListAsync();

            return View(premiumVideos);
        }

        // GET: Admin/Premium/Subscriptions
        public async Task<IActionResult> Subscriptions()
        {
            var subscriptions = await _context.Subscriptions
                .Include(s => s.User)
                .Include(s => s.Payments)
                .OrderByDescending(s => s.CreatedTime)
                .Select(s => new SubscriptionViewModel
                {
                    Id = s.Id,
                    UserName = s.User!.UserName ?? "",
                    Type = s.Type,
                    Status = s.Status,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Price = s.Price,
                    PaymentsCount = s.Payments!.Count()
                })
                .ToListAsync();

            return View(subscriptions);
        }

        // POST: Admin/Premium/ToggleUserPremium
        [HttpPost]
        public async Task<IActionResult> ToggleUserPremium(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.IsPremium = !user.IsPremium;
            if (!user.IsPremium)
            {
                user.PremiumExpiryDate = null;
            }
            else
            {
                user.PremiumExpiryDate = DateTime.Now.AddMonths(1); // Give 1 month free
            }

            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = $"User premium status updated successfully.";
            
            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/Premium/UpdateExpiredSubscriptions
        [HttpPost]
        public async Task<IActionResult> UpdateExpiredSubscriptions()
        {
            await _premiumAccessService.UpdateExpiredSubscriptionsAsync();
            TempData["SuccessMessage"] = "Expired subscriptions updated successfully.";
            
            return RedirectToAction(nameof(Index));
        }
    }

    // ViewModels for Admin Premium Management
    public class PremiumStatsViewModel
    {
        public int TotalPremiumUsers { get; set; }
        public int TotalPremiumVideos { get; set; }
        public int TotalActiveSubscriptions { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class PremiumUserViewModel
    {
        public string Id { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsPremium { get; set; }
        public DateTime? PremiumExpiryDate { get; set; }
        public Subscription? ActiveSubscription { get; set; }
    }

    public class PremiumVideoViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string AuthorName { get; set; } = "";
        public DateTime CreatedTime { get; set; }
        public int ViewCount { get; set; }
        public List<string> Categories { get; set; } = new();
    }

    public class SubscriptionViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = "";
        public SubscriptionType Type { get; set; }
        public SubscriptionStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Price { get; set; }
        public int PaymentsCount { get; set; }
    }
}