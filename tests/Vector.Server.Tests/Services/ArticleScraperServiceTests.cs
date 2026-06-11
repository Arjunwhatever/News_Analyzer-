using System.Net;
using Moq;
using Moq.Protected;
using Vector.Server.Services;
using Xunit;

namespace Vector.Server.Tests.Services
{
    public class ArticleScraperServiceTests
    {
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly HttpClient _httpClient;

        public ArticleScraperServiceTests()
        {
            _handlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_handlerMock.Object);
        }

        private ArticleScraperService CreateService()
        {
            return new ArticleScraperService(_httpClient);
        }

        [Fact]
        public async Task ExtractArticleTextAsync_ValidHtml_ExtractsTextAndIgnoresScripts()
        {
            // Arrange
            var html = @"
                <html>
                    <head><title>Test News</title></head>
                    <body>
                        <nav>Ignore this nav bar</nav>
                        <article>
                            <script>var x = 1;</script>
                            <p>This is the first valid paragraph of the article. It needs to be long enough to bypass the 40 char limit.</p>
                            <p>This is the second valid paragraph. Also sufficiently long enough to be included.</p>
                            <aside>Ignore sidebar</aside>
                        </article>
                        <footer>Ignore footer</footer>
                    </body>
                </html>";

            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(html)
                });

            var service = CreateService();

            // Act
            var result = await service.ExtractArticleTextAsync("http://example.com");

            // Assert
            Assert.Contains("This is the first valid paragraph", result);
            Assert.Contains("This is the second valid paragraph", result);
            Assert.DoesNotContain("var x = 1", result);
            Assert.DoesNotContain("Ignore this nav bar", result);
        }

        [Fact]
        public async Task ExtractArticleTextAsync_NoArticleTags_FallsBackToBody()
        {
            // Arrange
            var html = @"
                <html>
                    <body>
                        <p>This paragraph is just hanging out in the body. It must be over 40 characters so the filter doesn't eat it.</p>
                    </body>
                </html>";

            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(html)
                });

            var service = CreateService();

            // Act
            var result = await service.ExtractArticleTextAsync("http://example.com");

            // Assert
            Assert.Equal("This paragraph is just hanging out in the body. It must be over 40 characters so the filter doesn't eat it.", result.Trim());
        }
    }
}
