using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using vidoeMVC.DAL;
using vidoeMVC.Enums;
using vidoeMVC.Models;
using vidoeMVC.ViewModels;
using vidoeMVC.ViewModels.Categories;
using vidoeMVC.ViewModels.Users;
using vidoeMVC.ViewModels.Videos;
using vidoeMVC.Services;

namespace vidoeMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly VidoeDBContext _context;
        private readonly IPremiumAccessService _premiumAccessService;

        public HomeController(UserManager<AppUser> userManager, VidoeDBContext context, IPremiumAccessService premiumAccessService)
        {
            _userManager = userManager;
            _context = context;
            _premiumAccessService = premiumAccessService;
        }

        public async Task<IActionResult> Index(int? page)
        {
            const int pageSize = 8; // Number of items per page
            var categories = await _context.Categories.ToListAsync();

            // Get current user and check premium status
            var currentUserId = _userManager.GetUserId(User);
            var hasPremiumAccess = !string.IsNullOrEmpty(currentUserId) && await _premiumAccessService.HasPremiumAccessAsync(currentUserId);

            // Query videos with pagination - show public videos and premium videos for premium users
            var videosQuery = _context.Videos.AsQueryable();
            
            if (hasPremiumAccess)
            {
                // Premium users can see both public and premium videos
                videosQuery = videosQuery.Where(v => v.Privacy.Contains(VideoStatus.Public) || v.Privacy.Contains(VideoStatus.Premium));
            }
            else
            {
                // Non-premium users can only see public videos
                videosQuery = videosQuery.Where(v => v.Privacy.Contains(VideoStatus.Public));
            }

            var videos = await videosQuery
                .Include(v => v.Author)
                .OrderByDescending(v => v.CreatedTime)
                .Skip((page - 1 ?? 0) * pageSize)
                .Take(pageSize)
                .Select(v => new VideoViewModel
                {
                    Id = v.Id,
                    Title = v.Title,
                    TumbnailUrl = v.TumbnailUrl,
                    VideoUrl = v.VideoUrl,
                    CreatedTime = v.CreatedTime,
                    ViewCount = v.ViewCount,
                    Privacy = v.Privacy,
                    Author = new UserVM
                    {
                        UserName = v.Author.UserName,
                        Id = v.Author.Id
                    }
                }).ToListAsync();

            // Calculate total pages based on total videos count
            var totalVideos = await _context.Videos.CountAsync();
            var totalPages = (int)Math.Ceiling(totalVideos / (double)pageSize);

            // Pagination logic
            var pageNumber = page ?? 1; // Default to first page
            var paginationVM = new PaginationVM
            {
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            var appUsers = await _userManager.Users
                .Include(u => u.Followers)
                .ThenInclude(f => f.Follower)
                .Include(u => u.Followees)
                .ThenInclude(f => f.Followee)
                .OrderByDescending(u => u.Followers.Count)
                .Select(u => new UserVM
                {
                    UserName = u.UserName,
                    Id = u.Id,
                    ProfPhotURL = u.ProfilPhotoURL,
                    Followers = u.Followers ?? new List<UserFollow>(),
                    Followees = u.Followees ?? new List<UserFollow>()
                }).ToListAsync();

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

            var homeVM = new HomeVM
            {
                categories = categories.Select(c => new GetCategoryVM
                {
                    Name = c.Name,
                    Id = c.Id
                }).ToList(),
                users = appUsers,
                VideoViewModels = videos,
                paginationVM = paginationVM,
                UserL = userL
            };

            return View(homeVM);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            var appUsers = await _userManager.Users
                .Include(u => u.Followers)
                .ThenInclude(f => f.Follower)
                .Include(u => u.Followees)
                .ThenInclude(f => f.Followee)
                .Select(u => new UserVM
                {
                    ProfPhotURL = u.ProfilPhotoURL,
                    UserName = u.UserName,
                    Id = u.Id,
                    Followers = u.Followers ?? new List<UserFollow>(),
                    Followees = u.Followees ?? new List<UserFollow>()
                }).ToListAsync();

            if (string.IsNullOrWhiteSpace(query))
            {
                return View("Search", new HomeVM());
            }

            var videos = _context.Videos
                .Include(v => v.Author)
                .Where(v => v.Title.Contains(query) ||
                            v.Description.Contains(query) ||
                            v.Tags.Any(t => t.Tag.Name.Contains(query)) ||
                            v.VCategories.Any(c => c.Category.Name.Contains(query)) ||
                            v.Author.UserName.Contains(query))
                .Select(v => new VideoViewModel
                {
                    Id = v.Id,
                    Title = v.Title,
                    TumbnailUrl = v.TumbnailUrl,
                    VideoUrl = v.VideoUrl,
                    Description = v.Description,
                    CreatedTime = v.CreatedTime,
                    Author = new UserVM
                    {
                        UserName = v.Author.UserName,
                        Id = v.Author.Id
                    }
                })
                .ToList();
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

            var homeVM = new HomeVM
            {
                VideoViewModels = videos,
                users = appUsers,
                UserL= userL,
            };

            return View("Search", homeVM);
        }
    }
}
