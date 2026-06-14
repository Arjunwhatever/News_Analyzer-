using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Vector.Server.Controller;
using Vector.Server.Models;
using Vector.Server.Services;
using Xunit;

namespace Vector.Server.Tests.Controllers
{
    public class FeedControllerTests
    {
        private readonly Mock<ILiveNewsService> _newsServiceMock;
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly FeedController _controller;

        public FeedControllerTests()
        {
            _newsServiceMock = new Mock<ILiveNewsService>();
            _authServiceMock = new Mock<IAuthService>();
            _controller = new FeedController(_newsServiceMock.Object, _authServiceMock.Object);
        }

        [Fact]
        public async Task GetLiveNews_WithAuthenticatedUser_UsesTopics()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var topics = "AI, Tech";
            var expectedArticles = new List<LiveNewsArticle> 
            { 
                new LiveNewsArticle { Title = "AI News" } 
            };

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _authServiceMock.Setup(a => a.GetPreferencesAsync(userId)).ReturnsAsync(topics);
            _newsServiceMock.Setup(s => s.GetTopHeadlinesAsync(topics)).ReturnsAsync(expectedArticles);

            // Act
            var result = await _controller.GetLiveNews();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var articles = Assert.IsType<List<LiveNewsArticle>>(okResult.Value);
            Assert.Single(articles);
            Assert.Equal("AI News", articles[0].Title);

            _authServiceMock.Verify(a => a.GetPreferencesAsync(userId), Times.Once);
            _newsServiceMock.Verify(s => s.GetTopHeadlinesAsync(topics), Times.Once);
        }

        [Fact]
        public async Task GetLiveNews_WithoutAuthenticatedUser_UsesNullTopics()
        {
            // Arrange
            var expectedArticles = new List<LiveNewsArticle> 
            { 
                new LiveNewsArticle { Title = "General News" } 
            };

            // Unauthenticated user
            var user = new ClaimsPrincipal(new ClaimsIdentity());

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _newsServiceMock.Setup(s => s.GetTopHeadlinesAsync(null)).ReturnsAsync(expectedArticles);

            // Act
            var result = await _controller.GetLiveNews();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var articles = Assert.IsType<List<LiveNewsArticle>>(okResult.Value);
            Assert.Single(articles);
            Assert.Equal("General News", articles[0].Title);

            _authServiceMock.Verify(a => a.GetPreferencesAsync(It.IsAny<Guid>()), Times.Never);
            _newsServiceMock.Verify(s => s.GetTopHeadlinesAsync(null), Times.Once);
        }
    }
}
