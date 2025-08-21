using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using vidoeMVC.Models;
using vidoeMVC.ViewModels.Categories;
using vidoeMVC.ViewModels.Users;
using vidoeMVC.ViewModels;
using vidoeMVC.DAL;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using vidoeMVC.Services;
using vidoeMVC.Enums;

namespace vidoeMVC.Controllers
{
    public class VideoPageController(UserManager<AppUser> _userManager, VidoeDBContext _context, EmailService _emailService, IPremiumAccessService _premiumAccessService) : Controller
    {
        public async Task<IActionResult> Index(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var finduser = await _userManager.GetUserAsync(User);
            UserVM userL = null;
            if (finduser != null)
            {
                userL = await _userManager.Users.Select(u => new UserVM
                {
                    UserName = u.UserName,
                    Id = u.Id,
                    BirthDate = u.BirthDate,
                    Surname = u.Surname,
                    Name = u.Name,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    ProfPhotURL = u.ProfilPhotoURL
                }).FirstOrDefaultAsync(u => u.Id == finduser.Id);
            }

            var video = await _context.Videos
                .Include(c => c.Author)
                .Include(t => t.Tags).ThenInclude(t => t.Tag)
                .Include(c => c.VCategories).ThenInclude(c => c.Category)
                .Include(v => v.Casts).ThenInclude(vc => vc.User)
                .Include(a => a.Author).ThenInclude(a => a.Followers)
                .Include(v => v.Like).ThenInclude(vl => vl.LikeDislike)
                .Include(v => v.Comments).ThenInclude(vc => vc.Comment)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (video == null)
            {
                return NotFound();
            }

            // Check premium access for premium videos
            var currentUserId = _userManager.GetUserId(User);
            if (video.Privacy?.Contains(VideoStatus.Premium) == true)
            {
                var hasAccess = await _premiumAccessService.CanAccessVideoAsync(currentUserId ?? "", video);
                if (!hasAccess)
                {
                    TempData["ErrorMessage"] = "This is a premium video. Please subscribe to premium to access it.";
                    return RedirectToAction("Subscribe", "Payment");
                }
            }

            video.ViewCount++;
            _context.Update(video);
            await _context.SaveChangesAsync();

            var categoryIds = video.VCategories.Select(vc => vc.CategoryId).ToList();

            var relatedVideos = await _context.Videos
                .Include(v => v.Author)
                .Include(v => v.VCategories).ThenInclude(vc => vc.Category)
                .Where(v => v.VCategories.Any(vc => categoryIds.Contains(vc.CategoryId)))
                .ToListAsync();

            var appUsers = await _userManager.Users
                .Include(u => u.Followers)
                .ThenInclude(f => f.Follower)
                .Include(u => u.Followees)
                .ThenInclude(f => f.Followee)
                .Select(u => new UserVM
                {
                    ProfPhotURL=u.ProfilPhotoURL,
                    UserName = u.UserName,
                    Id = u.Id,
                    Followers = u.Followers ?? new List<UserFollow>(),
                    Followees = u.Followees ?? new List<UserFollow>()
                }).ToListAsync();

            video.Comments = video.Comments.OrderByDescending(c => c.Comment.CreatedTime).ToList();

            var homeVM = new HomeVM
            {
                users = appUsers,
                Video = video,
                Videos = relatedVideos,
                UserL=userL
            };

            return View(homeVM);
        }

        [HttpPost]
public async Task<IActionResult> Like(int videoId, bool isLike)
{
    var userId = _userManager.GetUserId(User);
    if (userId == null)
    {
        return Unauthorized();
    }

    var videoLike = await _context.VideoLikes
        .Include(vl => vl.LikeDislike)
        .FirstOrDefaultAsync(vl => vl.VideoId == videoId && vl.UserId == userId);

    if (videoLike != null)
    {
        videoLike.LikeDislike.IsLike = isLike;
    }
    else
    {
        var likeDislike = new LikeDislike { IsLike = isLike };
        _context.LikeDislikes.Add(likeDislike);
        await _context.SaveChangesAsync();

        videoLike = new VideoLike
        {
            VideoId = videoId,
            UserId = userId,
            LikeDislikeId = likeDislike.Id,
            LikeDislike = likeDislike
        };
        _context.VideoLikes.Add(videoLike);
    }

    await _context.SaveChangesAsync();

    return Ok();
}

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddComment(int videoId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return BadRequest();
            }

            var userId = _userManager.GetUserId(User);
            var comment = new Comment
            {
                Text = text,
                CreatedTime = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var videoComment = new VideoComment
            {
                VideoId = videoId,
                CommentId = comment.Id,
                UserId = userId
            };

            _context.VideoComments.Add(videoComment);
            await _context.SaveChangesAsync();

            var video = await _context.Videos
                .Include(v => v.Comments)
                    .ThenInclude(vc => vc.Comment)
                .Include(v => v.Comments)
                    .ThenInclude(vc => vc.User)
                .FirstOrDefaultAsync(v => v.Id == videoId);

            // Order comments by CreatedTime descending
            video.Comments = video.Comments.OrderByDescending(c => c.Comment.CreatedTime).ToList();

            return PartialView("_CommentsPartial", video.Comments);
        }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
        {
            return NotFound();
        }
            var userId = _userManager.GetUserId(User);
            var videoComment = await _context.VideoComments
                .Include(vc => vc.Comment)
                .FirstOrDefaultAsync(vc => vc.CommentId == commentId && vc.UserId == userId);

            if (videoComment == null)
            {
                return NotFound();
            }

        

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        // Optionally, remove VideoComment entry as well if necessary
        _context.VideoComments.Remove(videoComment);
        await _context.SaveChangesAsync();

        return Ok();
    }
        [HttpGet]
        public async Task<IActionResult> LoadMoreVideos(int skip, int videoId)
        {
            var relatedVideos = await _context.Videos
                .Include(v => v.Author)
                .Include(v => v.VCategories).ThenInclude(vc => vc.Category)
                .Where(v => v.Id != videoId) // Exclude the current video
                .Skip(skip)
                .Take(5) // Load next 5 videos
                .ToListAsync();

            return PartialView("_RelatedVideosPartial", relatedVideos);
        }

        [HttpPost]
        public async Task<IActionResult> ReportVideo([FromBody] VideoReport? model)
        {
            if (!ModelState.IsValid)
            {

                // Get the user ID of the currently logged-in user
                var userId = await _userManager.GetUserAsync(User);

                // Create a new video report
                var videoReport = new VideoReport
                {
                    VideoId = model.VideoId,
                    Reason = model.Reason,
                    UserId = userId.Id,
                    ReportedAt = DateTime.UtcNow
                };

                // Save the report to the database
                _context.VideoReports.Add(videoReport);
                await _context.SaveChangesAsync();

                // Send an email notification to admin users
                var video = await _context.Videos.FindAsync(model.VideoId);
                var user = await _context.Users.FindAsync(userId.Id);
                var emailMessage = $"User {user.UserName} reported video: '{video.Title}' reported video id: '{video.Id}' video Author: '{video.Author.UserName}' video author email: '{video.Author.Email}' " +
                    $" for the following reason: {model.Reason} " ;

                // Find all admin users
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in adminUsers)
                {
                    await _emailService.SendEmailAsync(admin.Email, "Video Report", emailMessage);
                }

                return Json(new { message = "Your report has been submitted." });
            }

            return Json(new { message = "There was an error submitting your report." });
        }

        //[Authorize]
        //[HttpPost]
        //public async Task<IActionResult> ReportVideo(int videoId, string reason)
        //{
        //    var userId = _userManager.GetUserId(User);

        //    if (userId == null || string.IsNullOrWhiteSpace(reason))
        //    {
        //        return BadRequest();
        //    }

        //    var videoReport = new VideoReport
        //    {
        //        VideoId = videoId,
        //        UserId = userId,
        //        Reason = reason,
        //        ReportedAt = DateTime.Now
        //    };

        //    _context.VideoReports.Add(videoReport);
        //    await _context.SaveChangesAsync();

        //    var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        //    foreach (var admin in adminUsers)
        //    {
        //        var message = $"A video has been reported.\n\nVideo ID: {videoId}\nReason: {reason}";
        //        await _emailService.SendEmailAsync(admin.Email, "Video Report Notification", message);
        //    }

        //    return Ok();
        //}

    }
}
public class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider, UserManager<AppUser> userManager)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure the admin role exists
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // Create an admin user if it doesn't already exist
        var adminEmail = "psyvemil@gmail.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new AppUser { UserName = adminEmail, Email = adminEmail };
            await userManager.CreateAsync(adminUser, "AdminPassword123!");
            await userManager.AddToRoleAsync(adminUser, "admin");
        }
    }
}
