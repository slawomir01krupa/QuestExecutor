using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Core.Contracts
{
    public sealed class AttemptSummary
    {
        public int Number { get; set; }                 
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset EndedAtUtc { get; set; }  
        public string Outcome { get; set; } = "Unknown";
        public string? Error { get; set; }
    }
}
