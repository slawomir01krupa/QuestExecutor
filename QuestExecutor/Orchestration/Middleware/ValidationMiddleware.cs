using FluentValidation;
using QuestExecutor.Core.Contracts;

namespace QuestExecutor.Api.Orchestration.Middleware
{
    public class ValidationMiddleware : IExecutionMiddleware
    {
        private readonly IValidator<ExecutionRequest> _validator;

        public ValidationMiddleware(IValidator<ExecutionRequest> validator)
        {
            _validator = validator;
        }

        public async Task<ExecutorOutcome> InvokeAsync(ExecutionRequest request, Func<ExecutionRequest, Task<ExecutorOutcome>> next)
        {
            var result = _validator.Validate(request);
            if (!result.IsValid)
            {
                return new ExecutorOutcome
                {
                    Success = false,
                    Error = string.Join("; ", result.Errors.Select(e => e.ErrorMessage))
                };
            }
            return await next(request);
        }
    }
}
