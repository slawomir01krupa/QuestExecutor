using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Core.Contracts
{
    public sealed class ExecutorOutcome
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public object? Payload { get; set; }
    }
}
