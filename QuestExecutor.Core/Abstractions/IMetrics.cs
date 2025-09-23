using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Core.Abstractions
{
    public interface IMetrics
    {
        void Inc(string name);
        void Observe(string name, long valueMs);
        string ExportText();
    }
}
