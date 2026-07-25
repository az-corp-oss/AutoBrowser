using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using AutoBrowser.Services;
using Serilog;
using Xunit;

namespace AutoBrowser.Tests.Services;

public class UpdateServiceTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsyncFunc;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsyncFunc)
        {
            _sendAsyncFunc = sendAsyncFunc;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _sendAsyncFunc(request, cancellationToken);
        }
    }

    public UpdateServiceTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .CreateLogger();
    }

    [Fact]
    public async Task CheckForUpdateAsync_404_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(async (req, ct) => 
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var client = new HttpClient(handler);
        var service = new UpdateService(client);

        var result = await service.CheckForUpdateAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_EmptyReleases_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(async (req, ct) => 
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            };
        });
        var client = new HttpClient(handler);
        var service = new UpdateService(client);

        var result = await service.CheckForUpdateAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_LatestRelease_ReturnsReleaseInfo()
    {
        var releases = new[]
        {
            new
            {
                tag_name = "v1.0.0",
                prerelease = false,
                html_url = "https://github.com/test/releases/tag/v1.0.0",
                assets = new[] { new { name = "AutoBrowser.zip", browser_download_url = "https://example.com/download" } }
            }
        };
        var json = JsonSerializer.Serialize(releases);
        
        var handler = new MockHttpMessageHandler(async (req, ct) => 
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });
        var client = new HttpClient(handler);
        var service = new UpdateService(client);

        var result = await service.CheckForUpdateAsync();
        Assert.NotNull(result);
        Assert.Equal(new Version(1, 0, 0), result.Version);
        Assert.False(result.IsPreRelease);
        Assert.Single(result.Assets);
    }

    [Fact]
    public async Task CheckForUpdateAsync_PrereleaseOnly_ReturnsPrerelease()
    {
        var releases = new[]
        {
            new
            {
                tag_name = "v2.0.0",
                prerelease = true,
                html_url = "https://github.com/test/releases/tag/v2.0.0",
                assets = Array.Empty<object>()
            }
        };
        var json = JsonSerializer.Serialize(releases);
        
        var handler = new MockHttpMessageHandler(async (req, ct) => 
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });
        var client = new HttpClient(handler);
        var service = new UpdateService(client);

        var result = await service.CheckForUpdateAsync();
        Assert.NotNull(result);
        Assert.Equal(new Version(2, 0, 0), result.Version);
        Assert.True(result.IsPreRelease);
    }

    [Fact]
    public async Task CheckForUpdateAsync_MultipleReleases_ReturnsLatest()
    {
        var releases = new[]
        {
            new
            {
                tag_name = "v1.0.0",
                prerelease = false,
                html_url = "https://github.com/test/releases/tag/v1.0.0",
                assets = Array.Empty<object>()
            },
            new
            {
                tag_name = "v2.1.0",
                prerelease = false,
                html_url = "https://github.com/test/releases/tag/v2.1.0",
                assets = Array.Empty<object>()
            },
            new
            {
                tag_name = "v2.0.0",
                prerelease = false,
                html_url = "https://github.com/test/releases/tag/v2.0.0",
                assets = Array.Empty<object>()
            }
        };
        var json = JsonSerializer.Serialize(releases);
        
        var handler = new MockHttpMessageHandler(async (req, ct) => 
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });
        var client = new HttpClient(handler);
        var service = new UpdateService(client);

        var result = await service.CheckForUpdateAsync();
        Assert.NotNull(result);
        Assert.Equal(new Version(2, 1, 0), result.Version);
    }

    [Fact]
    public async Task CheckForUpdateAsync_PrereleaseAndStableSameVersion_PrefersStable()
    {
        var releases = new[]
        {
            new
            {
                tag_name = "v2.0.0",
                prerelease = true,
                html_url = "https://github.com/test/releases/tag/v2.0.0",
                assets = Array.Empty<object>()
            },
            new
            {
                tag_name = "v2.0.0",
                prerelease = false,
                html_url = "https://github.com/test/releases/tag/v2.0.0",
                assets = Array.Empty<object>()
            }
        };
        var json = JsonSerializer.Serialize(releases);
        
        var handler = new MockHttpMessageHandler(async (req, ct) => 
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });
        var client = new HttpClient(handler);
        var service = new UpdateService(client);

        var result = await service.CheckForUpdateAsync();
        Assert.NotNull(result);
        Assert.Equal(new Version(2, 0, 0), result.Version);
        Assert.False(result.IsPreRelease);
    }
}
