using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Core.Contracts
{
    public class PowershellExecutorResult
    {
        public string? Command { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public string Output { get; set; }
    }
}
