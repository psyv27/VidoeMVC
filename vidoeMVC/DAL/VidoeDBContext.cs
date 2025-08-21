using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using vidoeMVC.Models;

namespace vidoeMVC.DAL
{
    public class VidoeDBContext : IdentityDbContext<AppUser>
    {
        public VidoeDBContext(DbContextOptions<VidoeDBContext> options) : base(options)
        {
        }

        public DbSet<Video> Videos { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<UserFollow> UserFollows { get; set; }
        public DbSet<VideoCategory> VideoCategories { get; set; }
        public DbSet<VideoTag> VideoTags { get; set; }
        public DbSet<VideoCast> VideoCasts { get; set; }
        public DbSet<LikeDislike> LikeDislikes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<VideoLike> VideoLikes { get; set; }
        public DbSet<VideoComment> VideoComments { get; set; }
        public DbSet<VideoReport> VideoReports { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserFollow>()
                .HasKey(uf => new { uf.FollowerId, uf.FolloweeId });

            modelBuilder.Entity<UserFollow>()
                .HasOne(uf => uf.Follower)
                .WithMany(u => u.Followees)
                .HasForeignKey(uf => uf.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFollow>()
                .HasOne(uf => uf.Followee)
                .WithMany(u => u.Followers)
                .HasForeignKey(uf => uf.FolloweeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VideoCategory>()
                .HasKey(vc => new { vc.VideoId, vc.CategoryId });

            modelBuilder.Entity<VideoCategory>()
                .HasOne(vc => vc.Video)
                .WithMany(v => v.VCategories)
                .HasForeignKey(vc => vc.VideoId);

            modelBuilder.Entity<VideoCategory>()
                .HasOne(vc => vc.Category)
                .WithMany(c => c.VideoCategories)
                .HasForeignKey(vc => vc.CategoryId);

            modelBuilder.Entity<VideoTag>()
                .HasKey(vt => new { vt.VideoId, vt.TagId });

            modelBuilder.Entity<VideoTag>()
                .HasOne(vt => vt.Video)
                .WithMany(v => v.Tags)
                .HasForeignKey(vt => vt.VideoId);

            modelBuilder.Entity<VideoTag>()
                .HasOne(vt => vt.Tag)
                .WithMany(t => t.VideoTags)
                .HasForeignKey(vt => vt.TagId);

            modelBuilder.Entity<VideoCast>()
                .HasKey(vc => new { vc.VideoId, vc.UserId });

            modelBuilder.Entity<VideoCast>()
                .HasOne(vc => vc.Video)
                .WithMany(v => v.Casts)
                .HasForeignKey(vc => vc.VideoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VideoCast>()
                .HasOne(vc => vc.User)
                .WithMany(u => u.VideoCasts)
                .HasForeignKey(vc => vc.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Videos)
                .WithOne(v => v.Author)
                .HasForeignKey(v => v.AuthorId);

            modelBuilder.Entity<VideoComment>()
                .HasKey(vc => new { vc.VideoId, vc.CommentId });

            modelBuilder.Entity<VideoComment>()
                .HasOne(vc => vc.Video)
                .WithMany(v => v.Comments)
                .HasForeignKey(vc => vc.VideoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<VideoLike>()
                .HasKey(vl => new { vl.VideoId, vl.LikeDislikeId });

            modelBuilder.Entity<VideoLike>()
                .HasOne(vl => vl.Video)
                .WithMany(v => v.Like)
                .HasForeignKey(vl => vl.VideoId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<VideoReport>(entity =>
            {
                entity.HasKey(vr => vr.Id);

                entity.HasOne(vr => vr.Video)
                    .WithMany(v => v.VideoReports)
                    .HasForeignKey(vr => vr.VideoId)
                    .OnDelete(DeleteBehavior.Restrict); // Изменение каскадного удаления на Restrict

                entity.HasOne(vr => vr.User)
                    .WithMany(u => u.VideoReports)
                    .HasForeignKey(vr => vr.UserId)
                    .OnDelete(DeleteBehavior.Restrict); // Изменение каскадного удаления на Restrict
            });

            // Payment entity configuration
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.Id);
                
                entity.Property(p => p.Amount)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(p => p.User)
                    .WithMany(u => u.Payments)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Subscription)
                    .WithMany(s => s.Payments)
                    .HasForeignKey("SubscriptionId")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Subscription entity configuration  
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.HasKey(s => s.Id);
                
                entity.Property(s => s.Price)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(s => s.User)
                    .WithMany(u => u.Subscriptions)
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
