using QuestExecutor.Core.Abstractions;
using QuestExecutor.Core.Contracts;
using System.Diagnostics;

namespace QuestExecutor.Api.Orchestration.Middleware
{
    public class MetricsMiddleware : IExecutionMiddleware
    {
        private readonly IMetrics _metrics;

        public MetricsMiddleware(IMetrics metrics)
        {
            _metrics = metrics;
        }

        public async Task<ExecutorOutcome> InvokeAsync(
            ExecutionRequest request,
            Func<ExecutionRequest, Task<ExecutorOutcome>> next)
        {
            _metrics.Inc("requests_total");
            var stopwatch = Stopwatch.StartNew();
            var outcome = await next(request);
            stopwatch.Stop();
            _metrics.Observe($"request_latency_ms {request.CorrelationId}", stopwatch.ElapsedMilliseconds);
            return outcome;
        }
    }
}
