using vidoeMVC.Enums;

namespace vidoeMVC.Models
{
    public class Subscription : BaseEntity
    {
        public new int Id { get; set; }
        public string UserId { get; set; }
        public SubscriptionType Type { get; set; }
        public SubscriptionStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Price { get; set; }
        public bool AutoRenew { get; set; } = true;
        public DateTime? CancelledDate { get; set; }
        public string? CancellationReason { get; set; }

        // Navigation properties
        public AppUser? User { get; set; }
        public ICollection<Payment>? Payments { get; set; }
    }
}