using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Vector.Server.Models;
using Vector.Server.Services;
using Xunit;

namespace Vector.Server.Tests.Services
{
    public class BiasAnalyzerServiceTests
    {
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<BiasAnalyzerService>> _loggerMock;
        private readonly HttpClient _httpClient;

        public BiasAnalyzerServiceTests()
        {
            _handlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_handlerMock.Object);

            _configMock = new Mock<IConfiguration>();
            var configSectionMock = new Mock<IConfigurationSection>();
            configSectionMock.Setup(s => s.Value).Returns("fake-api-key");
            _configMock.Setup(c => c.GetSection("AppSettings:OpenRouterApiKey")).Returns(configSectionMock.Object);
            _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<BiasAnalyzerService>>();
        }

        private BiasAnalyzerService CreateService()
        {
            return new BiasAnalyzerService(_httpClient, _configMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task AnalyzeAsync_ValidResponse_ReturnsAnalysisResult()
        {
            // Arrange
            var payload = new LlmAnalysisPayload
            {
                BiasScore = -3.5,
                Confidence = 0.9,
                Tone = "Analytical",
                KeyIndicators = new List<string> { "Indicator 1" },
                Summary = "Summary text",
                Topics = new List<string> { "Topic 1" }
            };

            var jsonResponse = JsonSerializer.Serialize(new
            {
                choices = new[]
                {
                    new { message = new { content = JsonSerializer.Serialize(payload) } }
                }
            });

            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var service = CreateService();

            // Act
            var result = await service.AnalyzeAsync("Some long article text here to bypass minimum length checks.");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-3.5, result.BiasScore);
            Assert.Equal("Analytical", result.Tone);
            Assert.Equal(0.9, result.Confidence);
        }

        [Fact]
        public async Task AnalyzeAsync_MarkdownFences_StripsFencesCorrectly()
        {
            // Arrange
            var payloadJson = "{\n  \"biasScore\": 5.0,\n  \"confidence\": 0.8,\n  \"tone\": \"Sensational\",\n  \"keyIndicators\": [],\n  \"summary\": \"Test\",\n  \"topics\": []\n}";
            var rawContent = $"```json\n{payloadJson}\n```";

            var jsonResponse = JsonSerializer.Serialize(new
            {
                choices = new[]
                {
                    new { message = new { content = rawContent } }
                }
            });

            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var service = CreateService();

            // Act
            var result = await service.AnalyzeAsync("Some long article text here.");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5.0, result.BiasScore);
            Assert.Equal("Sensational", result.Tone);
        }

        [Fact]
        public async Task AnalyzeAsync_ApiError_ThrowsHttpRequestException()
        {
            // Arrange
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    Content = new StringContent("Invalid API Key")
                });

            var service = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => service.AnalyzeAsync("Some valid text."));
        }
    }
}
