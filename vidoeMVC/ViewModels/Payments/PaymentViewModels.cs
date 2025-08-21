using System.ComponentModel.DataAnnotations;
using vidoeMVC.Enums;
using vidoeMVC.Models;

namespace vidoeMVC.ViewModels.Payments
{
    public class SubscribeViewModel
    {
        [Required]
        [Display(Name = "Subscription Type")]
        public SubscriptionType SubscriptionType { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; }

        [Display(Name = "Auto Renew")]
        public bool AutoRenew { get; set; } = true;

        // Display properties
        public bool CurrentlyPremium { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class ProcessPaymentViewModel
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = "";
        public PaymentMethod PaymentMethod { get; set; }
    }

    public class ManageSubscriptionViewModel
    {
        public bool IsPremium { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public Subscription? Subscription { get; set; }
    }
}