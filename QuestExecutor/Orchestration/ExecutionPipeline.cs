using QuestExecutor.Core.Contracts;

namespace QuestExecutor.Api.Orchestration
{
    public class ExecutionPipeline
    {
        private readonly IList<IExecutionMiddleware> _middlewares;

        public ExecutionPipeline(IEnumerable<IExecutionMiddleware> middlewares)
        {
            _middlewares = middlewares.ToList();
        }

        public Task<ExecutorOutcome> ExecuteAsync(
            ExecutionRequest request,
            Func<ExecutionRequest, Task<ExecutorOutcome>> finalHandler)
        {
            Func<ExecutionRequest, Task<ExecutorOutcome>> next = finalHandler;
            foreach (var middleware in _middlewares.Reverse())
            {
                var current = middleware;
                var prevNext = next;
                next = (req) => current.InvokeAsync(req, prevNext);
            }
            return next(request);
        }
    }
}
