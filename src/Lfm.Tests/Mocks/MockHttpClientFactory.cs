using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lfm.Tests.Mocks;

/// <summary>
/// Mock HTTP client factory for testing external API integrations
/// Allows setting up predefined responses for specific URLs without making real HTTP calls
/// </summary>
public class MockHttpClientFactory : IHttpClientFactory
{
    private readonly Dictionary<string, HttpResponseMessage> _responses = new();
    private readonly List<(string url, string method)> _callLog = new();

    /// <summary>
    /// Creates an HTTP client with mock responses
    /// </summary>
    public HttpClient CreateClient(string name)
    {
        return new HttpClient(new MockHttpMessageHandler(_responses, _callLog));
    }

    /// <summary>
    /// Sets up a mock response for a specific URL
    /// </summary>
    public void SetupResponse(string url, string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseContent)
        };
        _responses[url] = response;
    }

    /// <summary>
    /// Sets up a mock error response
    /// </summary>
    public void SetupErrorResponse(string url, HttpStatusCode statusCode, string errorMessage)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent($"{{\"error\": \"{errorMessage}\"}}")
        };
        _responses[url] = response;
    }

    /// <summary>
    /// Gets the number of times a URL was called
    /// </summary>
    public int GetCallCount(string url)
    {
        return _callLog.Count(x => x.url.Contains(url, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all recorded calls
    /// </summary>
    public IReadOnlyList<(string url, string method)> GetCallLog()
    {
        return _callLog.AsReadOnly();
    }

    /// <summary>
    /// Resets the call log
    /// </summary>
    public void ResetCallLog()
    {
        _callLog.Clear();
    }

    /// <summary>
    /// Resets all responses
    /// </summary>
    public void ResetResponses()
    {
        _responses.Clear();
    }

    /// <summary>
    /// Resets everything
    /// </summary>
    public void Reset()
    {
        ResetResponses();
        ResetCallLog();
    }

    /// <summary>
    /// Internal HTTP message handler that uses mock responses
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, HttpResponseMessage> _responses;
        private readonly List<(string, string)> _callLog;

        public MockHttpMessageHandler(Dictionary<string, HttpResponseMessage> responses, List<(string, string)> callLog)
        {
            _responses = responses;
            _callLog = callLog;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            _callLog.Add((request.RequestUri?.ToString() ?? "", request.Method?.ToString() ?? ""));

            var url = request.RequestUri?.ToString() ?? "";

            if (_responses.TryGetValue(url, out var response))
            {
                return Task.FromResult(response);
            }

            // Try to find a matching URL (for URLs with query parameters or slight variations)
            var matchingKey = _responses.Keys.FirstOrDefault(k => url.StartsWith(k, StringComparison.OrdinalIgnoreCase));
            if (matchingKey != null)
            {
                return Task.FromResult(_responses[matchingKey]);
            }

            // Return 404 if no mock response found
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent($"{{\"error\": \"No mock response configured for {url}\"}}")
            });
        }
    }
}
