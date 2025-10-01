using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Core.Contracts
{
    public sealed class ExecutionResultEnvelope
    {
        public string? RequestId { get; set; } 
        public string? CorrelationId { get; set; }
        public string? ExecutorType { get; set; }
        public string Status { get; set; } = "Unknown";
        public List<AttemptSummary> Attempts { get; set; } = new();
        public object? Result { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
