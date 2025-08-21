using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using vidoeMVC.DAL;
using vidoeMVC.Enums;
using vidoeMVC.Models;
using vidoeMVC.ViewModels.Payments;

namespace vidoeMVC.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly VidoeDBContext _context;
        private readonly UserManager<AppUser> _userManager;

        public PaymentController(VidoeDBContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Payment/Subscribe
        public async Task<IActionResult> Subscribe()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new SubscribeViewModel
            {
                CurrentlyPremium = user.IsPremium,
                ExpiryDate = user.PremiumExpiryDate
            };

            return View(model);
        }

        // POST: /Payment/Subscribe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(SubscribeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Create subscription
            var subscription = new Subscription
            {
                UserId = user.Id,
                Type = model.SubscriptionType,
                Status = SubscriptionStatus.PendingPayment,
                StartDate = DateTime.Now,
                EndDate = CalculateEndDate(model.SubscriptionType),
                Price = GetSubscriptionPrice(model.SubscriptionType),
                AutoRenew = model.AutoRenew,
                CreatedTime = DateTime.Now,
                UpdatedTime = DateTime.Now,
                IsDeleted = false
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            // Create payment record
            var payment = new Payment
            {
                UserId = user.Id,
                Amount = subscription.Price,
                Status = PaymentStatus.Pending,
                Method = model.PaymentMethod,
                PaymentDate = DateTime.Now,
                Description = $"{model.SubscriptionType} Premium Subscription",
                CreatedTime = DateTime.Now,
                UpdatedTime = DateTime.Now,
                IsDeleted = false
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Simulate payment processing (in real app, integrate with payment gateway)
            return RedirectToAction(nameof(ProcessPayment), new { paymentId = payment.Id });
        }

        // GET: /Payment/ProcessPayment/5
        public async Task<IActionResult> ProcessPayment(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null || payment.UserId != user.Id) return Forbid();

            var model = new ProcessPaymentViewModel
            {
                PaymentId = payment.Id,
                Amount = payment.Amount,
                Description = payment.Description ?? "",
                PaymentMethod = payment.Method
            };

            return View(model);
        }

        // POST: /Payment/CompletePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletePayment(int paymentId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null || payment.UserId != user.Id) return Forbid();

            // Simulate successful payment (in real app, verify with payment gateway)
            payment.Status = PaymentStatus.Completed;
            payment.TransactionId = Guid.NewGuid().ToString("N")[..8].ToUpper();
            payment.UpdatedTime = DateTime.Now;

            // Find and activate subscription
            var subscription = await _context.Subscriptions
                .Where(s => s.UserId == user.Id && s.Status == SubscriptionStatus.PendingPayment)
                .OrderByDescending(s => s.CreatedTime)
                .FirstOrDefaultAsync();

            if (subscription != null)
            {
                subscription.Status = SubscriptionStatus.Active;
                subscription.UpdatedTime = DateTime.Now;

                // Update user premium status
                user.IsPremium = true;
                user.PremiumExpiryDate = subscription.EndDate;
                await _userManager.UpdateAsync(user);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment completed successfully! You now have premium access.";
            return RedirectToAction(nameof(PaymentSuccess));
        }

        // GET: /Payment/PaymentSuccess
        public IActionResult PaymentSuccess()
        {
            return View();
        }

        // GET: /Payment/ManageSubscription
        public async Task<IActionResult> ManageSubscription()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var subscription = await _context.Subscriptions
                .Where(s => s.UserId == user.Id && s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            var model = new ManageSubscriptionViewModel
            {
                IsPremium = user.IsPremium,
                ExpiryDate = user.PremiumExpiryDate,
                Subscription = subscription
            };

            return View(model);
        }

        // POST: /Payment/CancelSubscription
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSubscription(int subscriptionId, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == user.Id);

            if (subscription == null) return NotFound();

            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelledDate = DateTime.Now;
            subscription.CancellationReason = reason;
            subscription.AutoRenew = false;
            subscription.UpdatedTime = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["InfoMessage"] = "Subscription cancelled. You'll have premium access until your current period expires.";
            return RedirectToAction(nameof(ManageSubscription));
        }

        private DateTime CalculateEndDate(SubscriptionType type)
        {
            return type switch
            {
                SubscriptionType.Monthly => DateTime.Now.AddMonths(1),
                SubscriptionType.Quarterly => DateTime.Now.AddMonths(3),
                SubscriptionType.Yearly => DateTime.Now.AddYears(1),
                _ => DateTime.Now.AddMonths(1)
            };
        }

        private decimal GetSubscriptionPrice(SubscriptionType type)
        {
            return type switch
            {
                SubscriptionType.Monthly => 9.99m,
                SubscriptionType.Quarterly => 24.99m,
                SubscriptionType.Yearly => 79.99m,
                _ => 9.99m
            };
        }
    }
}