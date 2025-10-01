using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestExecutor.Api.Orchestration.Middleware;
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

            var envelope = new ExecutionResultEnvelope
            {
                RequestId = req.RequestId.ToString(),
                CorrelationId = req.CorrelationId,
                ExecutorType = req.ExecutorType
            };

            var pipeline = new ExecutionPipeline(new IExecutionMiddleware[]
             {
                new LoggingMiddleware(_log),
                new MetricsMiddleware(_metrics),
                new ValidationMiddleware(_validator),
                new PolicyMiddleware(_policyRunner, _proxy.Value.Retry.MaxAttempts, TimeSpan.FromMilliseconds(_proxy.Value.DefaultTimeoutMs), _registry)
            });

            var outcome = await pipeline.ExecuteAsync(req, _ => Task.FromResult(new ExecutorOutcome { Success = false, Error = "No handler" }));

            if (!outcome.Success)
            {
                envelope.Status = "Failed";
                if (outcome.Error is not null)
                {
                    envelope.Errors.Add(outcome.Error);
                }
                _log.LogWarning(LogEvents.RequestFailure, "Request failed: {message}", outcome.Error);
                return envelope;
            }

            envelope.Status = "Success";
            envelope.Result = outcome.Payload;
            return envelope;
        }
    }
}
