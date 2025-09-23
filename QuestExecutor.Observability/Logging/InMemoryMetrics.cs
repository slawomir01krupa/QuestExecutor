using QuestExecutor.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestExecutor.Observability.Logging
{
    public class InMemoryMetrics : IMetrics
    {
        private readonly Dictionary<string, long> _counters = new();
        private readonly Dictionary<string, List<long>> _timers = new();
        private readonly object _gate = new();

        public string ExportText()
        {
            lock (_gate)
            {
                var lines = new List<string>();
                foreach (var kv in _counters) lines.Add($"counter{{name=\"{kv.Key}\"}} {kv.Value}");
                foreach (var kv in _timers)
                {
                    var arr = kv.Value.ToArray(); Array.Sort(arr);
                    var avg = arr.Length == 0 ? 0 : (long)arr.Average();
                    var p95 = arr.Length == 0 ? 0 : arr[(int)Math.Max(0, Math.Floor(arr.Length * 0.95) - 1)];
                    lines.Add($"timer_avg_ms{{name=\"{kv.Key}\"}} {avg}");
                    lines.Add($"timer_p95_ms{{name=\"{kv.Key}\"}} {p95}");
                }
                return string.Join("\n", lines);
            }
        }

        public void Inc(string name)
        {
            lock (_gate) _counters[name] = _counters.GetValueOrDefault(name) + 1;
        }

        public void Observe(string name, long valueMs)
        {
            lock (_gate)
            {
                if (!_timers.TryGetValue(name, out var list)) _timers[name] = list = new();
                list.Add(valueMs);
            }
        }
    }
}
