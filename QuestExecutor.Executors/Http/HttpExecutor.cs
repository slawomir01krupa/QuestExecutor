using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestExecutor.Core.Abstractions;
using QuestExecutor.Core.Contracts;
using QuestExecutor.Core.enums;
using QuestExecutor.Core.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace QuestExecutor.Executors.Http
{
    public class HttpExecutor : IExecutor
    {
        private readonly HttpClient _http;
        private ProxyOptions _proxy;
        private readonly ILogger<HttpExecutor> _log;

        public string ExecutorType => "http";

        public HttpExecutor(
        IHttpClientFactory httpClientFactory,
        IOptions<ProxyOptions> proxy,
        ILogger<HttpExecutor> log)
        {
            _http = httpClientFactory.CreateClient(nameof(HttpExecutor));
            _proxy = proxy.Value;
            _log = log;
        }

        public async Task<ExecutorOutcome> ExecuteAsync(ExecutionRequest req)
        {
            try
            {
                var uri = BuildUri(req.Target, req.Path, req.Query);

                using var msg = new HttpRequestMessage(new HttpMethod(req.Method), uri);

                if (req.Headers is not null)
                {
                    foreach (var (k, v) in req.Headers)
                    {
                        if (_proxy.AllowedHeaders.Contains(k, StringComparer.OrdinalIgnoreCase))
                            msg.Headers.TryAddWithoutValidation(k, v);
                    }
                }

                if (!string.IsNullOrEmpty(req.Body) &&
                    (req.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                     req.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
                     req.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase)))
                {
                    var contentType = "application/json";
                    if (req.Headers != null &&
                        req.Headers.TryGetValue("Content-Type", out var ct) &&
                        !string.IsNullOrWhiteSpace(ct))
                    {
                        contentType = ct;
                    }

                    msg.Content = new StringContent(req.Body, System.Text.Encoding.UTF8, contentType);
                }

                var start = DateTime.UtcNow;
                var resp = await _http.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead);
                var latencyMs = (long)(DateTime.UtcNow - start).TotalMilliseconds;

                var (preview, truncated) = await ReadPreviewAsync(resp, _proxy.MaxBodyBytes);

                var result = new HttpExecutorResult
                {
                    StatusCode = (int)resp.StatusCode,
                    Headers = resp.Headers.ToDictionary(k => k.Key, v => v.Value.ToArray()),
                    BodyPreview = preview,
                    BodyTruncated = truncated,
                    LatencyMs = latencyMs
                };

                return new ExecutorOutcome
                {
                    Success = resp.IsSuccessStatusCode,
                    Error = resp.IsSuccessStatusCode ? null : MapErrorCode(resp.StatusCode).ToString(),
                    Payload = result
                };
            }
            catch (HttpRequestException ex)
            {
                _log.LogWarning(ex, "HTTP request failed to {target}", req.Target);
                return new ExecutorOutcome
                {
                    Success = false,
                    Error = nameof(ErrorCode.TargetUnavailable)
                };
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected HTTP executor error");
                return new ExecutorOutcome
                {
                    Success = false,
                    Error = nameof(ErrorCode.Unknown)
                };
            }
        }

        private static Uri BuildUri(string baseUrl, string path, IDictionary<string, string>? q)
        {
            var baseUri = new Uri(baseUrl.TrimEnd('/'));
            var full = path.StartsWith("/") ? path : "/" + path;
            var ub = new UriBuilder(new Uri(baseUri, full));
            if (q is { Count: > 0 })
            {
                var queryString = string.Join("&",
                    q.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value ?? "")}"));
                ub.Query = queryString;
            }
            return ub.Uri;
        }

        private static async Task<(string text, bool truncated)> ReadPreviewAsync(HttpResponseMessage resp, int maxBytes)
        {
            if (resp.Content is null) return ("", false);

            using var stream = await resp.Content.ReadAsStreamAsync();
            using var ms = new MemoryStream();

            var buffer = new byte[4096];
            int total = 0, read;
            while ((read = await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, maxBytes - total))) > 0)
            {
                await ms.WriteAsync(buffer, 0, read);
                total += read;
                if (total >= maxBytes) break;
            }

            var preview = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            return (preview, total >= maxBytes);
        }

        private static ErrorCode MapErrorCode(System.Net.HttpStatusCode code) =>
        (int)code switch
        {
            400 => ErrorCode.InvalidSchema,
            401 => ErrorCode.Unauthorized,
            403 => ErrorCode.Forbidden,
            404 => ErrorCode.NotFound,
            408 => ErrorCode.Timeout,
            429 => ErrorCode.RateLimited,
            >= 500 => ErrorCode.Upstream5xx,
            _ => ErrorCode.Unknown
        };
    }
}
