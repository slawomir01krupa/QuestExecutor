using FluentValidation;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Options;
using QuestExecutor.Core.Contracts;
using System.Text.Json.Nodes;
using QuestExecutor.Core.Constants;
using QuestExecutor.Core.Options;

namespace QuestExecutor.Api.Validators
{
    public sealed class ExecutionRequestValidator : AbstractValidator<ExecutionRequest>
    {
        private static readonly string[] AllowedExecutors = { "http", "powershell" };
        private static readonly string[] AllowedHttpMethods = { "GET", "POST", "PUT", "PATCH", "DELETE" };

        private readonly ProxyOptions _proxy;

        public ExecutionRequestValidator(IOptions<ProxyOptions> proxy)
        {
            _proxy = proxy.Value;

            // --- Common rules ---
            RuleFor(x => x.RequestId)
                .NotEqual(Guid.Empty).WithMessage("RequestId must be a non-empty GUID.");

            RuleFor(x => x.CorrelationId)
                .MaximumLength(128);

            RuleFor(x => x.ExecutorType)
                .NotEmpty()
                .Must(t => AllowedExecutors.Contains(t, StringComparer.OrdinalIgnoreCase))
                .WithMessage(x => $"Unsupported executorType '{x.ExecutorType}'.");

            RuleFor(x => x.Method)
                .NotEmpty()
                .Must(m => AllowedHttpMethods.Contains(m, StringComparer.OrdinalIgnoreCase))
                .WithMessage(x => $"Unsupported HTTP method '{x.Method}'.");

            RuleFor(x => x.Path)
                .NotEmpty()
                .Must(p => p.StartsWith("/"))
                .WithMessage("Path must start with '/'.");

            RuleFor(x => x.Body)
                .Must((x, body) => BodyWithinLimit(body, _proxy.MaxBodyBytes))
                .WithMessage(x => $"Body exceeds configured limit ({_proxy.MaxBodyBytes} bytes).");

            RuleFor(x => x.Headers)
                .Must(h => h != null && h.ContainsKey(HeaderNames.TargetBase) && !string.IsNullOrWhiteSpace(h[HeaderNames.TargetBase]))
                .WithMessage($"Missing or empty required header '{HeaderNames.TargetBase}'.");

            RuleFor(x => x.Headers)
                .Must(h => h != null && h.ContainsKey(HeaderNames.CorrelationId) && !string.IsNullOrWhiteSpace(h[HeaderNames.CorrelationId]))
                .WithMessage($"Missing or empty required header '{HeaderNames.CorrelationId}'.");

            RuleFor(x => x.Headers)
                .Must(h => h != null && h.ContainsKey(HeaderNames.ExecutorType) && !string.IsNullOrWhiteSpace(h[HeaderNames.ExecutorType]))
                .WithMessage($"Missing or empty required header '{HeaderNames.ExecutorType}'.");

            // --- HTTP executor ---
            When(x => x.ExecutorType.Equals("http", StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(x => x.Target)
                    .NotEmpty().WithMessage("HTTP executor requires a 'Target'.")
                    .Must(IsValidHttpOrHttpsUri)
                    .WithMessage("HTTP executor requires a valid absolute http/https 'Target'.");

                RuleFor(x => x.Headers)
                    .Must(h => h == null || h.Keys.All(k => _proxy.AllowedHeaders.Contains(k, StringComparer.OrdinalIgnoreCase)))
                    .WithMessage("Contains headers not in AllowedHeaders.");

                // If Content-Type is JSON and a body is present, body must be valid JSON
                RuleFor(x => x)
                    .Must(BodyMatchesContentTypeIfJson)
                    .WithMessage("Body is not valid JSON for Content-Type application/json.");
            });

            // --- PowerShell executor ---
            When(x => x.ExecutorType.Equals("powershell", StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(x => x.Method)
                    .Equal("POST").WithMessage("PowerShell executor requires HTTP POST.");

                RuleFor(x => x.Body)
                    .NotEmpty().WithMessage("PowerShell executor requires a JSON body.")
                    .Must(IsValidJson).WithMessage("PowerShell executor requires a valid JSON body.");

                // Expect: { "command": "<allowlisted>", "parameters": { ... }? }
                RuleFor(x => x)
                    .Must(ValidatePowershellJsonAllowlist)
                    .WithMessage("PowerShell command invalid or not allowlisted.");
            });
        }

        // ---- helpers ----
        private static bool BodyWithinLimit(string body, int maxBytes)
            => (body ?? string.Empty).Length == 0
               || System.Text.Encoding.UTF8.GetByteCount(body) <= Math.Max(0, maxBytes);

        private static bool IsValidHttpOrHttpsUri(string s)
            => !string.IsNullOrWhiteSpace(s)
               && Uri.TryCreate(s, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        private static bool IsValidJson(string s)
        {
            try { if (string.IsNullOrWhiteSpace(s)) return false; JsonNode.Parse(s); return true; }
            catch { return false; }
        }

        private static bool BodyMatchesContentTypeIfJson(ExecutionRequest req)
        {
            if (req.Headers is null) return true;
            if (!req.Headers.TryGetValue("Content-Type", out var ct) || string.IsNullOrWhiteSpace(ct)) return true;
            if (!ct.Contains("application/json", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.IsNullOrWhiteSpace(req.Body)) return true;
            return IsValidJson(req.Body);
        }

        private static bool ValidatePowershellJsonAllowlist(ExecutionRequest req)
        {
            try
            {
                var node = JsonNode.Parse(req.Body!)!.AsObject();
                var command = node["command"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(command)) return false;

                // keep the allowlist small & explicit
                var allow = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "list-mailboxes", "list-users" };

                if (!allow.Contains(command)) return false;

                var parameters = node["parameters"];
                if (parameters is not null && parameters is not JsonObject) return false;

                return true;
            }
            catch { return false; }
        }
    }
}
