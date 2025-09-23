using QuestExecutor.Core.Abstractions;
using QuestExecutor.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Executors.Powershell
{
    public class PowershellExecutor : IExecutor
    {
        public string ExecutorType => "powershell";

        public Task<ExecutorOutcome> ExecuteAsync(ExecutionRequest req)
        {
            throw new NotImplementedException();
        }
    }
}
