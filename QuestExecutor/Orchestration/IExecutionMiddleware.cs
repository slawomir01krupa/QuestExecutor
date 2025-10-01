using QuestExecutor.Core.Contracts;

namespace QuestExecutor.Api.Orchestration
{
    public interface IExecutionMiddleware
    {
        Task<ExecutorOutcome> InvokeAsync(
        ExecutionRequest request,
        Func<ExecutionRequest, Task<ExecutorOutcome>> next);
    }
}
