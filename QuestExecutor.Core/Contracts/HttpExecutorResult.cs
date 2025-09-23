using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Core.Contracts
{
    public class HttpExecutorResult
    {
        public int StatusCode { get; set; }
        public Dictionary<string, string[]> Headers { get; set; } = new();
        public string BodyPreview { get; set; } = string.Empty;
        public bool BodyTruncated { get; set; }
        public long LatencyMs { get; set; }
    }
}
