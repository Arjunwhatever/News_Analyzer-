using Moq;
using Moq.Protected;
using System.Net;
using Microsoft.Extensions.Configuration;
using Vector.Server.Services;
using Xunit;

namespace Vector.Server.Tests.Services
{
    public class LiveNewsServiceTests
    {
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<IConfiguration> _configMock;

        public LiveNewsServiceTests()
        {
            _handlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_handlerMock.Object);

            _configMock = new Mock<IConfiguration>();
            var configSectionMock = new Mock<IConfigurationSection>();
            configSectionMock.Setup(s => s.Value).Returns("fake-api-key");
            _configMock.Setup(c => c.GetSection("AppSettings:NewsApiKey")).Returns(configSectionMock.Object);
        }

        private LiveNewsService CreateService()
        {
            return new LiveNewsService(_httpClient, _configMock.Object);
        }

        [Fact]
        public async Task GetTopHeadlinesAsync_NullTopics_CallsEverythingEndpointWithNewsQuery()
        {
            // Arrange
            var jsonResponse = @"{
                ""status"": ""ok"",
                ""articles"": [
                    { ""title"": ""Headline 1"", ""url"": ""http://example.com"" }
                ]
            }";

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Get && 
                        req.RequestUri!.ToString().StartsWith("https://newsapi.org/v2/everything")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var service = CreateService();

            // Act
            var articles = await service.GetTopHeadlinesAsync(null);

            // Assert
            Assert.Single(articles);
            Assert.Equal("Headline 1", articles[0].Title);
        }

        [Fact]
        public async Task GetTopHeadlinesAsync_WithTopics_CallsEverythingEndpoint()
        {
            // Arrange
            var topics = "AI, Space";
            var jsonResponse = @"{
                ""status"": ""ok"",
                ""articles"": [
                    { ""title"": ""Topic Article"", ""url"": ""http://example.com/topic"" }
                ]
            }";

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Get && 
                        req.RequestUri!.ToString().StartsWith("https://newsapi.org/v2/everything")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var service = CreateService();

            // Act
            var articles = await service.GetTopHeadlinesAsync(topics);

            // Assert
            Assert.Single(articles);
            Assert.Equal("Topic Article", articles[0].Title);
        }

        [Fact]
        public async Task GetTopHeadlinesAsync_FiltersOutRemovedArticles()
        {
            // Arrange
            var jsonResponse = @"{
                ""status"": ""ok"",
                ""articles"": [
                    { ""title"": ""Valid Article"", ""url"": ""http://example.com"" },
                    { ""title"": ""[Removed]"", ""url"": ""http://example.com/removed"" }
                ]
            }";

            _handlerMock
                .Protected()
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
            var articles = await service.GetTopHeadlinesAsync(null);

            // Assert
            Assert.Single(articles);
            Assert.Equal("Valid Article", articles[0].Title);
        }
    }
}
