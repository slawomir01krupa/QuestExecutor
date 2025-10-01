using QuestExecutor.Core.Abstractions;
using QuestExecutor.Core.Contracts;

namespace QuestExecutor.Api.Orchestration.Middleware
{
    public class PolicyMiddleware : IExecutionMiddleware
    {
        private readonly IPolicyRunner _policyRunner;
        private readonly int _maxAttempts;
        private readonly TimeSpan _timeout;
        private readonly IExecutorRegistry _registry;

        public PolicyMiddleware(IPolicyRunner policyRunner, int maxAttempts, TimeSpan timeout, IExecutorRegistry registry)
        {
            _policyRunner = policyRunner;
            _maxAttempts = maxAttempts;
            _timeout = timeout;
            _registry = registry;
        }

        public async Task<ExecutorOutcome> InvokeAsync(
        ExecutionRequest request,
        Func<ExecutionRequest, Task<ExecutorOutcome>> next)
        {
            var executor = _registry.Resolve(request.ExecutorType);
            if (executor == null)
            {
                return new ExecutorOutcome
                {
                    Success = false,
                    Error = $"No executor found for type '{request.ExecutorType}'."
                };
            }

            var (outcome, _) = await _policyRunner.ExecuteAsync(
                () => executor.ExecuteAsync(request),
                _maxAttempts,
                _timeout);

            return outcome;
        }
    }
}
