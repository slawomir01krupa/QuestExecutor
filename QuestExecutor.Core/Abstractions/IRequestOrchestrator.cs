using QuestExecutor.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Core.Abstractions
{
    public interface IRequestOrchestrator
    {
        Task<ExecutionResultEnvelope> HandleAsync(ExecutionRequest req);
    }
}
