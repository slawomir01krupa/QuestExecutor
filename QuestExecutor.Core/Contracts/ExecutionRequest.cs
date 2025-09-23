using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Core.Contracts
{

    public class ExecutionRequest
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public string CorrelationId { get; set; } = string.Empty;
        public string ExecutorType { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public Dictionary<string, string>? Query { get; set; } = new();
        public Dictionary<string, string>? Headers { get; set; } = new();
        public string Body { get; set; } = string.Empty;
    }
}

