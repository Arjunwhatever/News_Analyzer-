using Microsoft.AspNetCore.Mvc;
using Moq;
using Vector.Server.Controller;
using Vector.Server.Models;
using Vector.Server.Services;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace Vector.Server.Tests.Controllers
{
    public class AnalysisControllerTests
    {
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<IAuthService> _authServiceMock;

        public AnalysisControllerTests()
        {
            _handlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_handlerMock.Object);

            _configMock = new Mock<IConfiguration>();
            var configSectionMock = new Mock<IConfigurationSection>();
            configSectionMock.Setup(s => s.Value).Returns("fake-api-key");
            _configMock.Setup(c => c.GetSection("AppSettings:OpenRouterApiKey")).Returns(configSectionMock.Object);

            _authServiceMock = new Mock<IAuthService>();
        }

        private AnalysisController CreateController()
        {
            var biasService = new BiasAnalyzerService(_httpClient, _configMock.Object);
            var scraperService = new ArticleScraperService(_httpClient);
            return new AnalysisController(biasService, scraperService, _authServiceMock.Object);
        }

        [Fact]
        public async Task Analyze_EmptyPayload_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            var request = new AnalysisRequest { Text = null, Url = null };

            // Act
            var result = await controller.Analyze(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Either a URL or article text must be provided.", badRequest.Value);
        }

        [Fact]
        public async Task Analyze_TooShortText_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            var request = new AnalysisRequest { Text = "Too short" };

            // Act
            var result = await controller.Analyze(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Provided text is too short to perform a reliable bias analysis.", badRequest.Value);
        }
    }
}
