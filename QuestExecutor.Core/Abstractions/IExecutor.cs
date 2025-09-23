using QuestExecutor.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Core.Abstractions
{
    public interface IExecutor
    {
        string ExecutorType { get; }
        Task<ExecutorOutcome> ExecuteAsync(ExecutionRequest req);
    }
}
