using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace vidoeMVC.Models
{
    public class AppUser : IdentityUser
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public DateTime? BirthDate { get; set; }
        public virtual ICollection<UserFollow>? Followers { get; set; }
        public virtual ICollection<UserFollow>? Followees { get; set; }
        public ICollection<Video>? Videos { get; set; }
        public ICollection<VideoCast>? VideoCasts { get; set; }
        public string FBlink { get; set; } = "www.facebook.com";
        public string Xlink { get; set; } = "x.com";
        public string Instlink { get; set; } = "www.instagram.com";         
        public string? AboutMe { get; set; } = "No information heleki";
        public string? ProfilPhotoURL { get; set; } = "../imgs/DefaultProfPhot/Default.png";

        public ICollection<VideoComment>? VideoComments { get; set; }
        public ICollection<VideoLike>? VideoLike { get; set; }

        public ICollection<VideoReport>? VideoReports { get; set; }

        // Premium subscription properties
        public bool IsPremium { get; set; } = false;
        public DateTime? PremiumExpiryDate { get; set; }
        public ICollection<Subscription>? Subscriptions { get; set; }
        public ICollection<Payment>? Payments { get; set; }
    }
}
