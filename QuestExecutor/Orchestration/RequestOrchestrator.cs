using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestExecutor.Core.Abstractions;
using QuestExecutor.Core.Contracts;
using QuestExecutor.Core.Options;
using QuestExecutor.Observability.Logging;
using QuestExecutor.Resilience.Policies;
using System.Diagnostics;
using System.Threading;

namespace QuestExecutor.Api.Orchestration
{
    public class RequestOrchestrator : IRequestOrchestrator
    {
        private readonly IValidator<ExecutionRequest> _validator;
        private readonly IExecutorRegistry _registry;
        private readonly ILogger<RequestOrchestrator> _log;
        private readonly IMetrics _metrics;
        private readonly IPolicyRunner _policyRunner;
        private readonly IOptions<ProxyOptions> _proxy;

        public RequestOrchestrator(
            IValidator<ExecutionRequest> validator, 
            IExecutorRegistry registry,
            ILogger<RequestOrchestrator> log,
            IMetrics metrics,
            IPolicyRunner policyRunner,
            IOptions<ProxyOptions> proxy)
        {
            _validator = validator;
            _registry = registry;
            _log = log;
            _metrics = metrics;
            _policyRunner = policyRunner;
            _proxy = proxy;
        }
        public async Task<ExecutionResultEnvelope> HandleAsync(ExecutionRequest req)
        {
            using var scope = _log.BeginScope(new Dictionary<string, object?>
            {
                ["requestId"] = req.RequestId,
                ["correlationId"] = req.CorrelationId,
                ["executorType"] = req.ExecutorType,
                ["target"] = req.Target
            });

            _metrics.Inc("requests_total");

            _log.LogInformation(LogEvents.RequestStart, "Request start {method} {path}", req.Method, req.Path);

            var stopwatch = Stopwatch.StartNew();

            var start = DateTime.UtcNow;
            var envelope = new ExecutionResultEnvelope
            {
                RequestId = req.RequestId.ToString(),
                CorrelationId = req.CorrelationId,
                ExecutorType = req.ExecutorType,
                StartUtc = start
            };

            var result = _validator.Validate(req);
            if (!result.IsValid)
            {
                _log.LogWarning(
                    LogEvents.RequestInvalid, 
                    "Validation failed: {message}", 
                    string.Join("****", result.Errors.Select(e => e.ErrorMessage)));
                stopwatch.Stop();
                envelope.Errors = result.Errors.Select(e => e.ErrorMessage).ToList();
                envelope.ExecutionTimeMilliseconds = stopwatch.ElapsedMilliseconds;
                envelope.Status = "Failed";
                return envelope;
            }

            var executor = _registry.Resolve(req.ExecutorType);
            if(executor is null) {
                _log.LogWarning(
                   LogEvents.RequestInvalid,
                   "Validation failed: No Executor found");
                stopwatch.Stop();
                envelope.Errors.Add($"No executor found for type '{req.ExecutorType}'.");
                envelope.ExecutionTimeMilliseconds = stopwatch.ElapsedMilliseconds;
                envelope.Status = "Failed";
                return envelope;
            }

            var (outcome, attempts) = await _policyRunner.ExecuteAsync(
                attempt: () => executor.ExecuteAsync(req), 
                maxAttempts: _proxy.Value.Retry.MaxAttempts,
                perAttemptTimeout: TimeSpan.FromMilliseconds(_proxy.Value.DefaultTimeoutMs));


            envelope.Attempts = attempts;
            if (!outcome.Success)
            {
                envelope.Status = "Failed";
                if (outcome.Error is not null)
                {
                    envelope.Errors.Add(outcome.Error);
                }
                _log.LogWarning(LogEvents.RequestFailure, "Request failed: {message}", outcome.Error);
            }

            envelope.Status = "Success";
            envelope.Result = outcome.Payload;
            stopwatch.Stop();
            envelope.ExecutionTimeMilliseconds = stopwatch.ElapsedMilliseconds;
            _metrics.Observe($"request_latency_ms {req.CorrelationId}", stopwatch.ElapsedMilliseconds);
            return envelope;
        }
    }
}
