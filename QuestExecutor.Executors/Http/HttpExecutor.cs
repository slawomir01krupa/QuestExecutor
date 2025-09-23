using QuestExecutor.Core.Abstractions;
using QuestExecutor.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Executors.Http
{
    public class HttpExecutor : IExecutor
    {
        public string ExecutorType => "http";

        public Task<ExecutorOutcome> ExecuteAsync(ExecutionRequest req)
        {
            throw new NotImplementedException();
        }
    }
}
