namespace vidoeMVC.Enums
{
    public enum PaymentStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled,
        Refunded
    }

    public enum PaymentMethod
    {
        CreditCard,
        DebitCard,
        PayPal,
        BankTransfer,
        Crypto
    }

    public enum SubscriptionType
    {
        Monthly,
        Quarterly,
        Yearly
    }

    public enum SubscriptionStatus
    {
        Active,
        Expired,
        Cancelled,
        Suspended,
        PendingPayment
    }
}