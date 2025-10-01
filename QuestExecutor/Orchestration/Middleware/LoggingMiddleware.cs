using QuestExecutor.Core.Contracts;

namespace QuestExecutor.Api.Orchestration.Middleware
{
    public class LoggingMiddleware : IExecutionMiddleware
    {
        private readonly ILogger _log;

        public LoggingMiddleware(ILogger log)
        {
            _log = log;
        }

        public async Task<ExecutorOutcome> InvokeAsync(
            ExecutionRequest request,
            Func<ExecutionRequest, Task<ExecutorOutcome>> next)
        {
            _log.LogInformation("Request start {method} {path}", request.Method, request.Path);
            var outcome = await next(request);
            if (!outcome.Success)
            {
                _log.LogWarning("Request failed: {message}", outcome.Error);
            }
            return outcome;
        }
    }
}
