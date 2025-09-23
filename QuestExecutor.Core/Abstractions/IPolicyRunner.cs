using QuestExecutor.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Core.Abstractions
{
    public interface IPolicyRunner
    {
        Task<(ExecutorOutcome outcome, List<AttemptSummary> attempts)> ExecuteAsync(
        Func<Task<ExecutorOutcome>> attempt,
        int maxAttempts,
        TimeSpan perAttemptTimeout);
    }
}
