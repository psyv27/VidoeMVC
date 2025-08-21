using vidoeMVC.Enums;

namespace vidoeMVC.Models
{
    public class Payment : BaseEntity
    {
        public new int Id { get; set; }
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public PaymentMethod Method { get; set; }
        public string? TransactionId { get; set; }
        public string? PaymentGatewayResponse { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Description { get; set; }
        public int? SubscriptionId { get; set; }

        // Navigation properties
        public AppUser? User { get; set; }
        public Subscription? Subscription { get; set; }
    }
}