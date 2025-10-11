using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vidoeMVC.Controllers;
using vidoeMVC.DAL;
using vidoeMVC.Models;
using vidoeMVC.Services;
using vidoeMVC.ViewModels;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace vidoeMVC.Tests
{
    public class HomeControllerTests
    {
        private VidoeDBContext _context;
        private Mock<UserManager<AppUser>> _mockUserManager;
        private Mock<IPremiumAccessService> _mockPremiumAccessService;
        private HomeController _controller;

        private void Setup(string dbName)
        {
            var options = new DbContextOptionsBuilder<VidoeDBContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            _context = new VidoeDBContext(options);

            _mockUserManager = new Mock<UserManager<AppUser>>(Mock.Of<IUserStore<AppUser>>(), null, null, null, null, null, null, null, null);
            _mockPremiumAccessService = new Mock<IPremiumAccessService>();

            _controller = new HomeController(_mockUserManager.Object, _context, _mockPremiumAccessService.Object);
        }

        [Fact]
        public async Task Search_WhenNoQuery_ReturnsViewWithEmptyHomeVM()
        {
            // Arrange
            Setup("Search_WhenNoQuery_ReturnsViewWithEmptyHomeVM");

            // Act
            var result = await _controller.Search(null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<HomeVM>(viewResult.ViewData.Model);
            Assert.Null(model.VideoViewModels);
        }

        [Fact]
        public async Task Search_WhenQueryMatches_ReturnsViewWithMatchingVideos()
        {
            // Arrange
            Setup("Search_WhenQueryMatches_ReturnsViewWithMatchingVideos");
            var user = new AppUser { Id = "1", UserName = "testuser" };
            _context.Users.Add(user);
            _context.Videos.Add(new Video { Id = 1, Title = "Test Video", Description = "A video for testing", Author = user });
            _context.SaveChanges();

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            // Act
            var result = await _controller.Search("Test");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<HomeVM>(viewResult.ViewData.Model);
            Assert.Single(model.VideoViewModels);
            Assert.Equal("Test Video", model.VideoViewModels.First().Title);
        }
    }
}